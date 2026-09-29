using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Services;

public sealed class ReceiptIntegrationNotConfiguredException(string message) : Exception(message);

public sealed record ExtractedReceiptLine(string Description, int AmountCents);

public sealed record ReceiptExtraction(
    string? MerchantName,
    DateTime? TransactionDate,
    int? TotalAmountCents,
    IReadOnlyList<ExtractedReceiptLine> LineItems);

public interface IReceiptImageStore
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken);
}

public interface IReceiptAnalyzer
{
    Task<ReceiptExtraction> AnalyzeAsync(byte[] image, CancellationToken cancellationToken);
}

public sealed class AzureReceiptImageStore : IReceiptImageStore
{
    private readonly IConfiguration configuration;
    private readonly Lazy<BlobContainerClient> container;

    public AzureReceiptImageStore(IConfiguration configuration)
    {
        this.configuration = configuration;
        container = new Lazy<BlobContainerClient>(CreateContainerClient);
    }

    public async Task<string> UploadAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blobContainer = container.Value;
        await blobContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = blobContainer.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            },
            cancellationToken);
        return blobName;
    }

    public async Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken)
    {
        var blob = container.Value.GetBlobClient(blobName);
        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }

    private BlobContainerClient CreateContainerClient()
    {
        var containerName = configuration["Receipts:StorageContainer"];
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ReceiptIntegrationNotConfiguredException("Receipt storage is not configured.");

        var endpoint = configuration["Receipts:StorageAccountUri"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var storageUri))
            throw new ReceiptIntegrationNotConfiguredException("Receipt storage is not configured.");

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = configuration["AZURE_CLIENT_ID"],
        });
        return new BlobServiceClient(storageUri, credential).GetBlobContainerClient(containerName);
    }
}

public sealed class AzureReceiptAnalyzer : IReceiptAnalyzer
{
    private readonly IConfiguration configuration;
    private readonly Lazy<DocumentIntelligenceClient> client;

    public AzureReceiptAnalyzer(IConfiguration configuration)
    {
        this.configuration = configuration;
        client = new Lazy<DocumentIntelligenceClient>(CreateClient);
    }

    public async Task<ReceiptExtraction> AnalyzeAsync(byte[] image, CancellationToken cancellationToken)
    {
        var operation = await client.Value.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-receipt",
            BinaryData.FromBytes(image),
            cancellationToken: cancellationToken);

        var document = operation.Value.Documents.FirstOrDefault();
        if (document is null)
            return new ReceiptExtraction(null, null, null, []);

        var fields = document.Fields;
        var merchant = ReadString(fields, "MerchantName");
        var date = ReadDate(fields, "TransactionDate");
        var total = ReadAmount(fields, "Total");
        var lineItems = new List<ExtractedReceiptLine>();

        if (fields.TryGetValue("Items", out var itemsField) &&
            itemsField.FieldType == DocumentFieldType.List)
        {
            foreach (var item in itemsField.ValueList)
            {
                if (item.FieldType != DocumentFieldType.Dictionary)
                    continue;

                var itemFields = item.ValueDictionary;
                var description = ReadString(itemFields, "Description");
                var amount = ReadAmount(itemFields, "TotalPrice") ?? ReadAmount(itemFields, "Amount");
                if (!string.IsNullOrWhiteSpace(description) && amount is > 0)
                {
                    var amountCents = ToCents(amount.Value);
                    if (amountCents > 0)
                        lineItems.Add(new ExtractedReceiptLine(description, amountCents));
                }
            }
        }

        return new ReceiptExtraction(
            merchant,
            date,
            total is > 0 ? ToCents(total.Value) : null,
            lineItems);
    }

    private DocumentIntelligenceClient CreateClient()
    {
        var endpoint = configuration["Receipts:DocumentIntelligenceEndpoint"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var serviceUri))
            throw new ReceiptIntegrationNotConfiguredException("Receipt extraction is not configured.");

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = configuration["AZURE_CLIENT_ID"],
        });
        return new DocumentIntelligenceClient(serviceUri, credential);
    }

    private static string? ReadString(IReadOnlyDictionary<string, DocumentField> fields, string name)
        => fields.TryGetValue(name, out var field) && field.FieldType == DocumentFieldType.String
            ? field.ValueString
            : null;

    private static DateTime? ReadDate(IReadOnlyDictionary<string, DocumentField> fields, string name)
        => fields.TryGetValue(name, out var field) && field.FieldType == DocumentFieldType.Date
            ? field.ValueDate?.DateTime.Date
            : null;

    private static double? ReadAmount(IReadOnlyDictionary<string, DocumentField> fields, string name)
    {
        if (!fields.TryGetValue(name, out var field))
            return null;

        if (field.FieldType == DocumentFieldType.Double)
            return field.ValueDouble;
        if (field.FieldType == DocumentFieldType.Currency)
            return field.ValueCurrency.Amount;
        return null;
    }

    private static int ToCents(double amount)
    {
        var cents = Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        if (cents is < int.MinValue or > int.MaxValue)
            throw new InvalidDataException("The extracted receipt amount is outside the supported range.");
        return (int)cents;
    }
}
