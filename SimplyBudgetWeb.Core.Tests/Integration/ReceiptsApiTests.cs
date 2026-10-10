using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Azure;

using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Integration;

public class ReceiptsApiTests
{
    private static readonly byte[] JpegBytes = [0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46, 0x49, 0x46];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00];

    private static async Task<(ReceiptsApiFactory Factory, HttpClient Client)> CreateAsync()
    {
        var factory = new ReceiptsApiFactory();
        await factory.EnsureDatabaseAsync();
        return (factory, factory.CreateClient());
    }

    private static MultipartFormDataContent ImageForm(byte[] bytes, string fileName, string contentType, string field = "image")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, field, fileName } };
    }

    [Test]
    public async Task Upload_StoresImageExtractsDetailsAndAllowsReviewFlow()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;

        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ReceiptDto>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.MerchantName).IsEqualTo("Corner Cafe");
        await Assert.That(created.TotalAmountCents).IsEqualTo(1250);
        await Assert.That(created.ProcessingStatus).IsEqualTo("needs-review");
        await Assert.That(created.ProcessingMessage).IsNull();
        await Assert.That(created.LineItems.Length).IsEqualTo(2);
        await Assert.That(factory.ImageStore.Blobs.Single().Value).IsEquivalentTo(JpegBytes);

        var list = await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts");
        await Assert.That(list!.Single().Id).IsEqualTo(created.Id);

        var image = await client.GetAsync($"/api/receipts/{created.Id}/image");
        await Assert.That(image.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(image.Content.Headers.ContentType?.MediaType).IsEqualTo("image/jpeg");
        await Assert.That(await image.Content.ReadAsByteArrayAsync()).IsEquivalentTo(JpegBytes);

        var update = await client.PutAsJsonAsync($"/api/receipts/{created.Id}", new ReceiptUpdateRequest(
            "Fixed Cafe", new DateTime(2026, 3, 5), 1300, "note", []));
        await Assert.That(update.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<ReceiptDto>();
        await Assert.That(updated!.MerchantName).IsEqualTo("Fixed Cafe");
        await Assert.That(updated.LineItems.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Upload_AcceptsPng()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;

        using var form = ImageForm(PngBytes, "scan.png", "image/png");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task Upload_RequiresAuthentication()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.Authenticated = false;

        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [Arguments("receipt.gif", "image/gif")]
    [Arguments("receipt.jpg", "image/png")]
    [Arguments("receipt.txt", "text/plain")]
    public async Task Upload_RejectsUnsupportedTypes(string fileName, string contentType)
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;

        using var form = ImageForm(JpegBytes, fileName, contentType);
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(factory.ImageStore.Blobs.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Upload_RejectsContentThatIsNotAnImage()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;

        using var form = ImageForm([1, 2, 3, 4], "receipt.jpg", "image/jpeg");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Upload_RejectsMissingOrEmptyImage()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;

        using var wrongField = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg", field: "file");
        var missing = await client.PostAsync("/api/receipts", wrongField);
        using var empty = ImageForm([], "receipt.jpg", "image/jpeg");
        var emptyResponse = await client.PostAsync("/api/receipts", empty);

        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(emptyResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Upload_ReturnsServiceUnavailableWhenStorageIsNotConfigured()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.ImageStore.UploadException = new ReceiptIntegrationNotConfiguredException("Receipt storage is not configured.");

        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("Receipt storage is not configured.");
    }

    [Test]
    public async Task Upload_KeepsReceiptForManualReviewWhenExtractionFails()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.Analyzer.Exception = new RequestFailedException("boom");

        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ReceiptDto>();
        await Assert.That(created!.ProcessingStatus).IsEqualTo("needs-review");
        await Assert.That(created.ProcessingMessage).Contains("Automatic extraction failed");
    }

    [Test]
    public async Task Upload_KeepsReceiptForManualReviewWhenAnalyzerIsNotConfigured()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.Analyzer.Exception = new ReceiptIntegrationNotConfiguredException("Receipt analysis is not configured.");

        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ReceiptDto>();
        await Assert.That(created!.ProcessingMessage).IsEqualTo("Receipt analysis is not configured.");
    }
}
