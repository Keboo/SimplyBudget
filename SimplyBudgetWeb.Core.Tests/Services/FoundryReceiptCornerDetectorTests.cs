using System.Net;
using System.Text.Json.Nodes;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Services;

public class FoundryReceiptCornerDetectorTests
{
    private static readonly byte[] Image = [0xff, 0xd8, 0xff, 0xe0];

    private static string ChatResponse(string content)
        => new JsonObject
        {
            ["choices"] = new JsonArray(new JsonObject
            {
                ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = content },
            }),
        }.ToJsonString();

    private static (FoundryReceiptCornerDetector Detector, RecordingHandler Handler) Create(
        HttpStatusCode status,
        string responseBody,
        Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["Receipts:FoundryEndpoint"] = "https://example.services.ai.azure.com/",
                ["Receipts:FoundryCornerDeployment"] = "gpt-4.1",
            })
            .Build();
        var handler = new RecordingHandler(status, responseBody);
        var factory = Mock.Of<IHttpClientFactory>(x => x.CreateClient(It.IsAny<string>()) == new HttpClient(handler));
        return (new FoundryReceiptCornerDetector(configuration, factory, new StaticTokenCredential()), handler);
    }

    [Test]
    public async Task DetectAsync_SendsImageToFoundryDeploymentAndMapsPerMilleCorners()
    {
        var (detector, handler) = Create(HttpStatusCode.OK, ChatResponse("""
            {"receiptFound":true,"headerSide":"top","imageTopLeft":{"x":200,"y":100},"imageTopRight":{"x":800,"y":120},"imageBottomRight":{"x":780,"y":900},"imageBottomLeft":{"x":220,"y":880}}
            """));

        var geometry = await detector.DetectAsync(Image, "image/jpeg", CancellationToken.None);

        await Assert.That(handler.Request!.RequestUri!.ToString())
            .IsEqualTo("https://example.services.ai.azure.com/openai/v1/chat/completions");
        await Assert.That(handler.Request.Headers.Authorization!.ToString()).IsEqualTo("Bearer test-token");
        var body = JsonNode.Parse(handler.Body!)!;
        await Assert.That(body["model"]!.GetValue<string>()).IsEqualTo("gpt-4.1");
        await Assert.That(body["response_format"]!["json_schema"]!["strict"]!.GetValue<bool>()).IsTrue();
        await Assert.That(body["messages"]![1]!["content"]![1]!["image_url"]!["url"]!.GetValue<string>())
            .IsEqualTo($"data:image/jpeg;base64,{Convert.ToBase64String(Image)}");

        await Assert.That(geometry).IsNotNull();
        await Assert.That(geometry!.ToJson()).IsEqualTo(
            """{"version":1,"corners":[{"x":0.2,"y":0.1},{"x":0.8,"y":0.12},{"x":0.78,"y":0.9},{"x":0.22,"y":0.88}]}""");
    }

    [Test]
    public async Task ParseCorners_OrientsByHeaderSideReordersMislabeledCornersAndReturnsNullWhenNoReceiptIsFound()
    {
        // Text running down the photo with its header on the right: the receipt's top-left is the
        // photo's top-right paper corner.
        var sideways = FoundryReceiptCornerDetector.ParseCorners("""
            {"receiptFound":true,"headerSide":"right","imageTopLeft":{"x":100,"y":200},"imageTopRight":{"x":900,"y":200},"imageBottomRight":{"x":900,"y":600},"imageBottomLeft":{"x":100,"y":600}}
            """);
        await Assert.That(sideways!.ToJson()).IsEqualTo(
            """{"version":1,"corners":[{"x":0.9,"y":0.2},{"x":0.9,"y":0.6},{"x":0.1,"y":0.6},{"x":0.1,"y":0.2}]}""");

        // Counter-clockwise (mirrored) labels, with the model's top-left kept as the first corner.
        var mirrored = FoundryReceiptCornerDetector.ParseCorners("""
            {"receiptFound":true,"headerSide":"top","imageTopLeft":{"x":200,"y":100},"imageTopRight":{"x":220,"y":880},"imageBottomRight":{"x":780,"y":900},"imageBottomLeft":{"x":800,"y":120}}
            """);
        await Assert.That(mirrored!.IsValid()).IsTrue();
        await Assert.That(mirrored.Corners[0]).IsEqualTo(new ReceiptImagePoint(0.2, 0.1));
        await Assert.That(mirrored.Corners[1]).IsEqualTo(new ReceiptImagePoint(0.8, 0.12));

        var clamped = FoundryReceiptCornerDetector.ParseCorners("""
            {"receiptFound":true,"headerSide":"top","imageTopLeft":{"x":-20,"y":0},"imageTopRight":{"x":1100,"y":0},"imageBottomRight":{"x":1000,"y":1000},"imageBottomLeft":{"x":0,"y":1000}}
            """);
        await Assert.That(clamped!.Corners[0]).IsEqualTo(new ReceiptImagePoint(0, 0));
        await Assert.That(clamped.Corners[1]).IsEqualTo(new ReceiptImagePoint(1, 0));

        await Assert.That(FoundryReceiptCornerDetector.ParseCorners("""
            {"receiptFound":false,"headerSide":"top","imageTopLeft":{"x":0,"y":0},"imageTopRight":{"x":0,"y":0},"imageBottomRight":{"x":0,"y":0},"imageBottomLeft":{"x":0,"y":0}}
            """)).IsNull();
        await Assert.That(FoundryReceiptCornerDetector.ParseCorners("""
            {"receiptFound":true,"headerSide":"top","imageTopLeft":{"x":500,"y":500},"imageTopRight":{"x":500,"y":500},"imageBottomRight":{"x":500,"y":500},"imageBottomLeft":{"x":500,"y":500}}
            """)).IsNull();
    }

    [Test]
    public async Task DetectAsync_ThrowsForMissingConfigurationAndFailedOrMalformedResponses()
    {
        var (unconfigured, unconfiguredHandler) = Create(HttpStatusCode.OK, "", new Dictionary<string, string?>());
        await Assert.ThrowsAsync<ReceiptIntegrationNotConfiguredException>(
            () => unconfigured.DetectAsync(Image, "image/jpeg", CancellationToken.None));
        await Assert.That(unconfiguredHandler.Request).IsNull();

        var (failing, _) = Create(HttpStatusCode.TooManyRequests, """{"error":"rate limited"}""");
        await Assert.ThrowsAsync<ReceiptCornerDetectionFailedException>(
            () => failing.DetectAsync(Image, "image/jpeg", CancellationToken.None));

        var (malformed, _) = Create(HttpStatusCode.OK, ChatResponse("not json"));
        await Assert.ThrowsAsync<ReceiptCornerDetectionFailedException>(
            () => malformed.DetectAsync(Image, "image/jpeg", CancellationToken.None));
    }

    private sealed class RecordingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }

    private sealed class StaticTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new("test-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }
}
