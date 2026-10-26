using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Services;

public sealed class ReceiptCornerDetectionFailedException(string message, Exception? inner = null) : Exception(message, inner);

public interface IReceiptCornerDetector
{
    /// <summary>
    /// Finds the four corners of the receipt paper in an (already EXIF-oriented) image.
    /// Returns null when no receipt could be located.
    /// </summary>
    Task<ReceiptImageGeometry?> DetectAsync(byte[] image, string contentType, CancellationToken cancellationToken);
}

/// <summary>
/// Uses a vision-capable chat model deployed in Microsoft Foundry (for example gpt-4.1 or gpt-5)
/// to locate the receipt. The model only returns corner coordinates; the crop itself is a
/// deterministic perspective transform of the original pixels, so receipt content is never
/// generated or altered by AI.
/// </summary>
public sealed class FoundryReceiptCornerDetector : IReceiptCornerDetector
{
    public const string HttpClientName = "foundry-receipt-corners";
    private const string TokenScope = "https://cognitiveservices.azure.com/.default";
    private const double Scale = 1000;

    private const string Instructions = """
        You locate a paper receipt in a photo so it can be cropped and perspective-corrected.
        Return the four corners of the physical receipt paper itself: its outer paper edges,
        including the blank margins around and below the printed text. Do not outline just the
        printed text block, and do not return the photo frame. Exclude the table, hands, and any
        other background. If the receipt is longer than the photo, use the points where the
        paper edges leave the image. If the receipt is folded or curled, use the outermost
        corners that enclose the paper.
        Coordinates are integers from 0 to 1000, where (0,0) is the top-left of the image and
        (1000,1000) is the bottom-right; x grows to the right, y grows downward.
        Name each corner by where it sits in the image as shown (imageTopLeft is the paper corner
        nearest the image's top-left), regardless of which way the text reads.
        Separately, set headerSide to the side of the image ("top", "right", "bottom" or "left")
        that the top of the receipt (the store name / header, where reading starts) points toward.
        For a receipt lying upright it is "top"; if the text runs downward with the header on
        the right, it is "right".
        Set receiptFound to false (and return zeros) when no receipt or paper document is visible.
        """;

    private static readonly JsonObject ResponseFormat = new()
    {
        ["type"] = "json_schema",
        ["json_schema"] = new JsonObject
        {
            ["name"] = "receipt_corners",
            ["strict"] = true,
            ["schema"] = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray("receiptFound", "headerSide", "imageTopLeft", "imageTopRight", "imageBottomRight", "imageBottomLeft"),
                ["properties"] = new JsonObject
                {
                    ["receiptFound"] = new JsonObject { ["type"] = "boolean" },
                    ["headerSide"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("top", "right", "bottom", "left") },
                    ["imageTopLeft"] = Point(),
                    ["imageTopRight"] = Point(),
                    ["imageBottomRight"] = Point(),
                    ["imageBottomLeft"] = Point(),
                },
            },
        },
    };
    private readonly IConfiguration configuration;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly Lazy<TokenCredential> credential;

    public FoundryReceiptCornerDetector(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        TokenCredential? credential = null)
    {
        this.configuration = configuration;
        this.httpClientFactory = httpClientFactory;
        this.credential = new Lazy<TokenCredential>(() => credential ?? new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { ManagedIdentityClientId = configuration["AZURE_CLIENT_ID"] }));
    }

    public async Task<ReceiptImageGeometry?> DetectAsync(byte[] image, string contentType, CancellationToken cancellationToken)
    {
        var endpoint = configuration["Receipts:FoundryEndpoint"];
        var deployment = configuration["Receipts:FoundryCornerDeployment"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var serviceUri) || string.IsNullOrWhiteSpace(deployment))
            throw new ReceiptIntegrationNotConfiguredException("Automatic receipt cropping is not configured.");

        var token = await credential.Value.GetTokenAsync(new TokenRequestContext([TokenScope]), cancellationToken);
        var body = new JsonObject
        {
            ["model"] = deployment,
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = Instructions },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray(
                        new JsonObject { ["type"] = "text", ["text"] = "Find the receipt corners in this photo." },
                        new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JsonObject
                            {
                                ["url"] = $"data:{contentType};base64,{Convert.ToBase64String(image)}",
                                ["detail"] = "high",
                            },
                        }),
                }),
            ["response_format"] = ResponseFormat.DeepClone(),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(serviceUri, "openai/v1/chat/completions"))
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        string content;
        try
        {
            using var response = await httpClientFactory.CreateClient(HttpClientName)
                .SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new ReceiptCornerDetectionFailedException(
                    $"Foundry returned {(int)response.StatusCode}: {Truncate(responseText)}");
            content = ReadMessageContent(responseText);
        }
        catch (HttpRequestException exception)
        {
            throw new ReceiptCornerDetectionFailedException("Foundry could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ReceiptCornerDetectionFailedException("Foundry did not respond in time.", exception);
        }

        return ParseCorners(content);
    }

    public static ReceiptImageGeometry? ParseCorners(string modelJson)
    {
        try
        {
            using var document = JsonDocument.Parse(modelJson);
            var root = document.RootElement;
            if (!root.GetProperty("receiptFound").GetBoolean())
                return null;

            ReceiptImagePoint Read(string name)
            {
                var point = root.GetProperty(name);
                return new ReceiptImagePoint(
                    Math.Clamp(point.GetProperty("x").GetDouble() / Scale, 0, 1),
                    Math.Clamp(point.GetProperty("y").GetDouble() / Scale, 0, 1));
            }

            ReceiptImagePoint[] corners = [Read("imageTopLeft"), Read("imageTopRight"), Read("imageBottomRight"), Read("imageBottomLeft")];
            if (!new ReceiptImageGeometry(1, corners).IsValid())
            {
                // The model sometimes mislabels corners (e.g. mirrored or crossed). Re-order them
                // clockwise around their centroid while keeping its image top-left first.
                corners = OrderClockwise(corners);
            }

            // The receipt's top edge runs clockwise along the image side its header points to,
            // so its top-left is the first corner of that side.
            var headerSide = root.GetProperty("headerSide").GetString();
            var start = headerSide switch
            {
                "top" => 0,
                "right" => 1,
                "bottom" => 2,
                "left" => 3,
                _ => throw new FormatException($"Unsupported header side {headerSide}."),
            };
            var geometry = new ReceiptImageGeometry(1, [.. Enumerable.Range(0, 4).Select(i => corners[(start + i) % 4])]);
            return geometry.IsValid() ? geometry : null;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new ReceiptCornerDetectionFailedException("Foundry returned an unexpected corner format.", exception);
        }
    }

    private static ReceiptImagePoint[] OrderClockwise(ReceiptImagePoint[] corners)
    {
        var cx = corners.Average(p => p.X);
        var cy = corners.Average(p => p.Y);
        // With y pointing down, increasing atan2 sweeps clockwise on screen.
        var sorted = corners.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
        var start = sorted.IndexOf(corners[0]);
        return Enumerable.Range(0, 4).Select(i => sorted[(start + i) % 4]).ToArray();
    }

    private static string ReadMessageContent(string responseText)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            var message = document.RootElement.GetProperty("choices")[0].GetProperty("message");
            if (message.TryGetProperty("refusal", out var refusal) && refusal.ValueKind == JsonValueKind.String)
                throw new ReceiptCornerDetectionFailedException("Foundry declined to analyze the image.");
            return message.GetProperty("content").GetString()
                ?? throw new ReceiptCornerDetectionFailedException("Foundry returned no corners.");
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new ReceiptCornerDetectionFailedException("Foundry returned an unexpected response.", exception);
        }
    }

    private static JsonObject Point() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new JsonArray("x", "y"),
        ["properties"] = new JsonObject
        {
            ["x"] = new JsonObject { ["type"] = "integer" },
            ["y"] = new JsonObject { ["type"] = "integer" },
        },
    };

    private static string Truncate(string value) => value.Length <= 300 ? value : value[..300];
}
