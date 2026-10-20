using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;

using Azure;
using Azure.Identity;

using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Integration;

public class ReceiptsApiTests
{
    private const string GeometryJson = """{"version":1,"corners":[{"x":0.15,"y":0.1},{"x":0.9,"y":0.2},{"x":0.8,"y":0.95},{"x":0.05,"y":0.85}]}""";
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
        form.Add(new StringContent(GeometryJson), "imageGeometry");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ReceiptDto>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.MerchantName).IsEqualTo("Corner Cafe");
        await Assert.That(created.TotalAmountCents).IsEqualTo(1250);
        await Assert.That(created.ProcessingStatus).IsEqualTo("needs-review");
        await Assert.That(created.ProcessingMessage).IsNull();
        await Assert.That(created.LineItems.Length).IsEqualTo(2);
        await Assert.That(created.ImageGeometry!.ToJson()).IsEqualTo(GeometryJson);
        await Assert.That(factory.ImageStore.Blobs.Single().Value).IsEquivalentTo(JpegBytes);

        var list = await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts");
        await Assert.That(list!.Single().Id).IsEqualTo(created.Id);
        await Assert.That(list!.Single().ImageGeometry!.ToJson()).IsEqualTo(GeometryJson);
        var reloaded = await client.GetFromJsonAsync<ReceiptDto>($"/api/receipts/{created.Id}");
        await Assert.That(reloaded!.ImageGeometry!.ToJson()).IsEqualTo(GeometryJson);

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
        await Assert.That(updated.ImageGeometry!.ToJson()).IsEqualTo(GeometryJson);
        var persisted = await client.GetFromJsonAsync<ReceiptDto>($"/api/receipts/{created.Id}");
        await Assert.That(persisted!.ImageGeometry!.ToJson()).IsEqualTo(GeometryJson);
        await Assert.That(factory.ImageStore.Blobs.Single().Value).IsEquivalentTo(JpegBytes);
    }

    [Test]
    public async Task Upload_AcceptsPng()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;

        using var form = ImageForm(PngBytes, "scan.png", "image/png");
        var response = await client.PostAsync("/api/receipts", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var receipt = await response.Content.ReadFromJsonAsync<ReceiptDto>();
        await Assert.That(receipt!.ImageGeometry).IsNull();
    }

    [Test]
    [Arguments("""{"version":1,"corners":null}""")]
    [Arguments("""{"version":1,"corners":[null,null,null,null]}""")]
    [Arguments("""{"version":1,"corners":[]}""")]
    [Arguments("""{"version":2,"corners":[{"x":0,"y":0},{"x":1,"y":0},{"x":1,"y":1},{"x":0,"y":1}]}""")]
    [Arguments("""{"version":1,"corners":[{},{"x":1,"y":0},{"x":1,"y":1},{"x":0,"y":1}]}""")]
    [Arguments("""{"version":1,"corners":[{"x":0,"y":0},{"x":1,"y":1},{"x":1,"y":0},{"x":0,"y":1}]}""")]
    [Arguments("""{"version":1,"corners":[{"x":0,"y":0},{"x":0,"y":1},{"x":1,"y":1},{"x":1,"y":0}]}""")]
    [Arguments("""{"version":1,"corners":[{"x":0,"y":0},{"x":1,"y":0},{"x":0.2,"y":0.1},{"x":1,"y":1}]}""")]
    [Arguments("""{"version":1,"corners":[{"x":0,"y":0},{"x":0.001,"y":0},{"x":0.001,"y":1},{"x":0,"y":1}]}""")]
    [Arguments("""{"version":1,"corners":[{"x":-0.1,"y":0},{"x":1,"y":0},{"x":1,"y":1},{"x":0,"y":1}]}""")]
    [Arguments("""{"version":1,"corners":[{"x":1e999,"y":0},{"x":1,"y":0},{"x":1,"y":1},{"x":0,"y":1}]}""")]
    [Arguments("""{"version":1,"corners":[{"x":"NaN","y":0},{"x":1,"y":0},{"x":1,"y":1},{"x":0,"y":1}]}""")]
    [Arguments("{")]
    [Arguments("null")]
    public async Task Upload_RejectsMalformedGeometryBeforeStoringOriginal(string json)
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        form.Add(new StringContent(json), "imageGeometry");
        var response = await client.PostAsync("/api/receipts", form);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(factory.ImageStore.Blobs.Count).IsEqualTo(0);
        await Assert.That((await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Length).IsEqualTo(0);
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

    [Test]
    [Arguments("request")]
    [Arguments("configuration")]
    [Arguments("authentication")]
    public async Task Delete_RemovesTheImageAndReceiptOnlyAfterStorageSucceeds(string failure)
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var upload = await client.PostAsync("/api/receipts", form);
        var receipt = (await upload.Content.ReadFromJsonAsync<ReceiptDto>())!;

        factory.ImageStore.DeleteException = failure switch
        {
            "configuration" => new ReceiptIntegrationNotConfiguredException("Receipt storage is not configured."),
            "authentication" => new AuthenticationFailedException("Storage access denied"),
            _ => new RequestFailedException("Storage unavailable"),
        };
        var failed = await client.DeleteAsync($"/api/receipts/{receipt.Id}");
        await Assert.That(failed.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await Assert.That((await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Single().Id).IsEqualTo(receipt.Id);
        await Assert.That(factory.ImageStore.Blobs.Count).IsEqualTo(1);
        await Assert.That(factory.MonthNotifier.Months.Count).IsEqualTo(0);

        factory.ImageStore.DeleteException = null;
        var deleted = await client.DeleteAsync($"/api/receipts/{receipt.Id}");
        await Assert.That(deleted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That((await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Length).IsEqualTo(0);
        await Assert.That(factory.ImageStore.Blobs.Count).IsEqualTo(0);
        await Assert.That((await client.GetAsync($"/api/receipts/{receipt.Id}/image")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(factory.MonthNotifier.Months).IsEquivalentTo([new DateTime(2026, 3, 1)]);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetWebContext>();
        await Assert.That(await db.ReceiptLineItems.CountAsync()).IsEqualTo(0);
        await Assert.That((await client.DeleteAsync($"/api/receipts/{receipt.Id}")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Delete_RejectsProcessingReceiptsWithoutTouchingStorage()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.Analyzer.Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var uploading = client.PostAsync("/api/receipts", form);
        await factory.Analyzer.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var receipt = (await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Single();
        try
        {
            var response = await client.DeleteAsync($"/api/receipts/{receipt.Id}");
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
            await Assert.That(factory.ImageStore.Blobs.Count).IsEqualTo(1);
            await Assert.That((await client.GetFromJsonAsync<ReceiptDto>($"/api/receipts/{receipt.Id}"))!.ProcessingStatus).IsEqualTo("processing");
        }
        finally
        {
            factory.Analyzer.Completion.TrySetResult(factory.Analyzer.Result);
            await uploading;
        }
    }

    [Test]
    public async Task Delete_FinishesDatabaseCleanupWhenCanceledAfterStorageAndIncludesNewLinks()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var upload = await client.PostAsync("/api/receipts", form);
        var receipt = (await upload.Content.ReadFromJsonAsync<ReceiptDto>())!;
        using var cancellation = new CancellationTokenSource();
        int expenseId = 0;
        factory.ImageStore.AfterDelete = async () =>
        {
            using var linkScope = factory.Services.CreateScope();
            var db = linkScope.ServiceProvider.GetRequiredService<BudgetWebContext>();
            var expense = new ExpenseCategoryItem { Date = new DateTime(2026, 4, 6), Description = "Concurrent expense" };
            db.ExpenseCategoryItems.Add(expense);
            await db.SaveChangesAsync();
            expenseId = expense.ID;
            db.ReceiptExpenseLinks.Add(new ReceiptExpenseLink { ReceiptId = receipt.Id, ExpenseCategoryItemId = expense.ID });
            await db.SaveChangesAsync();
            cancellation.Cancel();
        };

        using var scope = factory.Services.CreateScope();
        var controller = ActivatorUtilities.CreateInstance<ReceiptsController>(scope.ServiceProvider);
        var deleted = await controller.Delete(receipt.Id, cancellation.Token);
        await Assert.That(deleted).IsTypeOf<Microsoft.AspNetCore.Mvc.NoContentResult>();
        var verificationDb = scope.ServiceProvider.GetRequiredService<BudgetWebContext>();
        await Assert.That(await verificationDb.Receipts.CountAsync()).IsEqualTo(0);
        await Assert.That(await verificationDb.ReceiptExpenseLinks.CountAsync()).IsEqualTo(0);
        await Assert.That(await verificationDb.ReceiptLineItems.CountAsync()).IsEqualTo(0);
        await Assert.That((await verificationDb.ExpenseCategoryItems.SingleAsync()).ID).IsEqualTo(expenseId);
        await Assert.That(factory.MonthNotifier.Months).IsEquivalentTo([new DateTime(2026, 3, 1), new DateTime(2026, 4, 1)]);
    }

    [Test]
    public async Task Upload_LateAnalysisDoesNotRecreateAReviewedThenDeletedReceipt()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.Analyzer.Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var uploading = client.PostAsync("/api/receipts", form);
        await factory.Analyzer.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var receipt = (await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Single();
        try
        {
            await client.PutAsJsonAsync($"/api/receipts/{receipt.Id}", new ReceiptUpdateRequest(
                "Reviewed", new DateTime(2026, 3, 8), 999, null, []));
            await Assert.That((await client.DeleteAsync($"/api/receipts/{receipt.Id}")).StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        }
        finally
        {
            factory.Analyzer.Completion.TrySetResult(factory.Analyzer.Result);
        }
        await Assert.That((await uploading).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetWebContext>();
        await Assert.That(await db.Receipts.CountAsync()).IsEqualTo(0);
        await Assert.That(await db.ReceiptLineItems.CountAsync()).IsEqualTo(0);
        await Assert.That(factory.ImageStore.Blobs.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Upload_LateAnalysisDoesNotOverwriteSavedManualCorrections(bool failAnalysis)
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.Analyzer.Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var uploading = client.PostAsync("/api/receipts", form);
        await factory.Analyzer.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var receipt = (await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Single();
        await Assert.That(receipt.ProcessingStatus).IsEqualTo("processing");
        var correction = await client.PutAsJsonAsync($"/api/receipts/{receipt.Id}", new ReceiptUpdateRequest(
            "Manual merchant", new DateTime(2026, 3, 8), 999, "Manual note", [new("Manual item", 999)]));
        await Assert.That(correction.StatusCode).IsEqualTo(HttpStatusCode.OK);
        if (failAnalysis)
            factory.Analyzer.Completion.SetException(new RequestFailedException("Analysis failed"));
        else
            factory.Analyzer.Completion.SetResult(factory.Analyzer.Result);
        var completed = await uploading;
        var returned = (await completed.Content.ReadFromJsonAsync<ReceiptDto>())!;
        await Assert.That(returned.MerchantName).IsEqualTo("Manual merchant");
        await Assert.That(returned.TotalAmountCents).IsEqualTo(999);
        await Assert.That(returned.TransactionDate).IsEqualTo(new DateTime(2026, 3, 8));
        await Assert.That(returned.ProcessingStatus).IsEqualTo("reviewed");
        await Assert.That(returned.LineItems.Single().Description).IsEqualTo("Manual item");
        var persisted = (await client.GetFromJsonAsync<ReceiptDto>($"/api/receipts/{receipt.Id}"))!;
        await Assert.That(persisted.MerchantName).IsEqualTo("Manual merchant");
        await Assert.That(persisted.TotalAmountCents).IsEqualTo(999);
    }

    [Test]
    [Arguments("wrong-total")]
    [Arguments("missing-date")]
    [Arguments("far-away-date")]
    public async Task Convert_ExplicitReceiptOutsideSuggestions_LinksHistoryAndPreservesRequest(string receiptData)
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        factory.Analyzer.Result = factory.Analyzer.Result with
        {
            TotalAmountCents = receiptData == "wrong-total" ? 9900 : 1250,
            TransactionDate = receiptData switch
            {
                "missing-date" => null,
                "far-away-date" => new DateTime(2025, 6, 1),
                _ => new DateTime(2026, 3, 4),
            },
        };
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        var upload = await client.PostAsync("/api/receipts", form);
        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var receipt = (await upload.Content.ReadFromJsonAsync<ReceiptDto>())!;

        int pendingId;
        int categoryId;
        string version;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BudgetWebContext>();
            var category = new ExpenseCategory { Name = "Food", CurrentBalance = 10000 };
            var pending = new PendingExpense { Date = new DateTime(2026, 3, 4), Description = "Bank merchant", Amount = 1250, IsDebit = true };
            db.ExpenseCategories.Add(category);
            db.PendingExpenses.Add(pending);
            await db.SaveChangesAsync();
            pendingId = pending.ID;
            categoryId = category.ID;
            version = Convert.ToBase64String(pending.Version);
        }

        var matches = await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts/matches?amount=1250&date=2026-03-04");
        await Assert.That(matches!.Length).IsEqualTo(0);
        var available = (await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Single();
        await Assert.That(available.Id).IsEqualTo(receipt.Id);
        await Assert.That(available.IsLinked).IsFalse();

        var conversion = await client.PostAsJsonAsync($"/api/pending-expenses/{pendingId}/convert", new ConvertPendingExpenseRequest(
            "My edited expense", new DateTime(2026, 4, 6), [new ConvertPendingExpenseItemRequest(categoryId, 1400)], version,
            Notes: "My expense note", ReceiptId: receipt.Id));
        await Assert.That(conversion.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var expense = (await client.GetFromJsonAsync<HistoryItemDto[]>("/api/history?month=2026-04-01"))!.Single();
        await Assert.That(expense.Description).IsEqualTo("My edited expense");
        await Assert.That(expense.Date).IsEqualTo(new DateTime(2026, 4, 6));
        await Assert.That(expense.Notes).IsEqualTo("My expense note");
        await Assert.That(expense.Details.Single().Amount).IsEqualTo(-1400);
        await Assert.That(expense.Details.Single().ExpenseCategoryId).IsEqualTo(categoryId);
        await Assert.That(expense.Receipt!.Id).IsEqualTo(receipt.Id);
        await Assert.That(expense.Receipt.IsLinked).IsTrue();
        await Assert.That(expense.Receipt.TotalAmountCents).IsEqualTo(receipt.TotalAmountCents);
        await Assert.That(expense.Receipt.TransactionDate).IsEqualTo(receipt.TransactionDate);
        await Assert.That((await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Single().IsLinked).IsTrue();

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BudgetWebContext>();
        await Assert.That(await verificationDb.PendingExpenses.CountAsync()).IsEqualTo(0);
        var link = await verificationDb.ReceiptExpenseLinks.SingleAsync();
        await Assert.That(link.ReceiptId).IsEqualTo(receipt.Id);
        await Assert.That(link.ExpenseCategoryItemId).IsEqualTo(expense.Id);
        await Assert.That((await verificationDb.ExpenseCategories.SingleAsync()).CurrentBalance).IsEqualTo(8600);
    }

    [Test]
    [Arguments("nonexistent")]
    [Arguments("already-used")]
    [Arguments("credit")]
    public async Task Convert_ExplicitReceipt_RejectsInvalidLinksWithoutChangingExpenses(string invalidLink)
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        int pendingId;
        int categoryId;
        int receiptId;
        int? existingExpenseId = null;
        string version;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BudgetWebContext>();
            var category = new ExpenseCategory { Name = "Food", CurrentBalance = 10000 };
            var pending = new PendingExpense
            {
                Date = new DateTime(2026, 3, 4), Description = "Bank merchant", Amount = 1250,
                IsDebit = invalidLink != "credit",
            };
            var receipt = new Receipt
            {
                FileName = "receipt.jpg", BlobName = "receipt.jpg",
                TotalAmountCents = 1250, TransactionDate = pending.Date,
            };
            db.ExpenseCategories.Add(category);
            db.PendingExpenses.Add(pending);
            if (invalidLink != "nonexistent")
                db.Receipts.Add(receipt);
            await db.SaveChangesAsync();
            if (invalidLink == "already-used")
            {
                var existing = new ExpenseCategoryItem { Date = pending.Date, Description = "Existing expense" };
                db.ExpenseCategoryItems.Add(existing);
                await db.SaveChangesAsync();
                existingExpenseId = existing.ID;
                db.ReceiptExpenseLinks.Add(new ReceiptExpenseLink { ReceiptId = receipt.Id, ExpenseCategoryItemId = existing.ID });
                await db.SaveChangesAsync();
            }
            pendingId = pending.ID;
            categoryId = category.ID;
            receiptId = invalidLink == "nonexistent" ? 999 : receipt.Id;
            version = Convert.ToBase64String(pending.Version);
        }

        var response = await client.PostAsJsonAsync($"/api/pending-expenses/{pendingId}/convert", new ConvertPendingExpenseRequest(
            "My expense", new DateTime(2026, 3, 4), [new ConvertPendingExpenseItemRequest(categoryId, 1250)], version,
            ReceiptId: receiptId));
        await Assert.That(response.StatusCode).IsEqualTo(invalidLink == "already-used" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest);
        var expectedError = invalidLink switch
        {
            "nonexistent" => "The selected receipt was not found.",
            "already-used" => "The selected receipt is already linked to an expense.",
            _ => "Receipts can only be linked to pending expenses.",
        };
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains(expectedError);

        using var verificationScope = factory.Services.CreateScope();
        var dbAfter = verificationScope.ServiceProvider.GetRequiredService<BudgetWebContext>();
        await Assert.That((await dbAfter.PendingExpenses.SingleAsync()).ID).IsEqualTo(pendingId);
        await Assert.That(await dbAfter.ExpenseCategoryItems.CountAsync()).IsEqualTo(existingExpenseId.HasValue ? 1 : 0);
        await Assert.That(await dbAfter.ExpenseCategoryItemDetails.CountAsync()).IsEqualTo(0);
        await Assert.That((await dbAfter.ExpenseCategories.SingleAsync()).CurrentBalance).IsEqualTo(10000);
        var links = await dbAfter.ReceiptExpenseLinks.ToArrayAsync();
        await Assert.That(links.Length).IsEqualTo(existingExpenseId.HasValue ? 1 : 0);
        if (existingExpenseId.HasValue)
        {
            await Assert.That(links.Single().ReceiptId).IsEqualTo(receiptId);
            await Assert.That(links.Single().ExpenseCategoryItemId).IsEqualTo(existingExpenseId.Value);
        }
        await Assert.That(factory.MonthNotifier.Months.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ReceiptLifecycle_CategorizedDeletionPreservesExpenseAndRefreshesHistory()
    {
        var (factory, client) = await CreateAsync();
        using var _ = factory;
        using var form = ImageForm(JpegBytes, "receipt.jpg", "image/jpeg");
        form.Add(new StringContent(GeometryJson), "imageGeometry");
        var upload = await client.PostAsync("/api/receipts", form);
        var receipt = (await upload.Content.ReadFromJsonAsync<ReceiptDto>())!;
        var corrected = await client.PutAsJsonAsync($"/api/receipts/{receipt.Id}", new ReceiptUpdateRequest(
            "Corrected merchant", new DateTime(2026, 3, 5), 1300, "Original receipt note", []));
        await Assert.That(corrected.StatusCode).IsEqualTo(HttpStatusCode.OK);

        int pendingId;
        int categoryId;
        string version;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BudgetWebContext>();
            var category = new ExpenseCategory { Name = "Food" };
            var pending = new PendingExpense { Date = new DateTime(2026, 3, 4), Description = "Bank merchant", Amount = 1300, IsDebit = true };
            db.ExpenseCategories.Add(category);
            db.PendingExpenses.Add(pending);
            await db.SaveChangesAsync();
            pendingId = pending.ID;
            categoryId = category.ID;
            version = Convert.ToBase64String(pending.Version);
        }

        var matches = await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts/matches?amount=1300&date=2026-03-04");
        await Assert.That(matches!.Single().Id).IsEqualTo(receipt.Id);
        await Assert.That(matches!.Single().IsLinked).IsFalse();
        await Assert.That(matches!.Single().ImageGeometry!.ToJson()).IsEqualTo(GeometryJson);
        var conversion = await client.PostAsJsonAsync($"/api/pending-expenses/{pendingId}/convert", new ConvertPendingExpenseRequest(
            "My expense description", new DateTime(2026, 4, 6), [new ConvertPendingExpenseItemRequest(categoryId, 1300)], version,
            Notes: "My expense note", ReceiptId: receipt.Id));
        await Assert.That(conversion.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var history = await client.GetFromJsonAsync<HistoryItemDto[]>("/api/history?month=2026-04-01");
        var expense = history!.Single();
        await Assert.That(expense.Description).IsEqualTo("My expense description");
        await Assert.That(expense.Date).IsEqualTo(new DateTime(2026, 4, 6));
        await Assert.That(expense.Notes).IsEqualTo("My expense note");
        await Assert.That(expense.Receipt!.Id).IsEqualTo(receipt.Id);
        await Assert.That(expense.Receipt.MerchantName).IsEqualTo("Corrected merchant");
        await Assert.That(expense.Receipt.IsLinked).IsTrue();
        await Assert.That(expense.Receipt.ImageGeometry!.ToJson()).IsEqualTo(GeometryJson);
        await Assert.That((await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts"))!.Single().IsLinked).IsTrue();
        await Assert.That((await client.GetFromJsonAsync<ReceiptDto[]>("/api/receipts/matches?amount=1300&date=2026-03-04"))!.Length).IsEqualTo(0);
        factory.MonthNotifier.Months.Clear();
        factory.ImageStore.DeleteException = new RequestFailedException("Storage unavailable");
        var failed = await client.DeleteAsync($"/api/receipts/{receipt.Id}");
        await Assert.That(failed.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await Assert.That(await client.GetByteArrayAsync($"/api/receipts/{receipt.Id}/image")).IsEquivalentTo(JpegBytes);
        using (var failedScope = factory.Services.CreateScope())
        {
            var db = failedScope.ServiceProvider.GetRequiredService<BudgetWebContext>();
            await Assert.That(await db.Receipts.CountAsync()).IsEqualTo(1);
            await Assert.That(await db.ReceiptExpenseLinks.CountAsync()).IsEqualTo(1);
        }
        await Assert.That(factory.MonthNotifier.Months.Count).IsEqualTo(0);

        factory.ImageStore.DeleteException = null;
        await Assert.That((await client.DeleteAsync($"/api/receipts/{receipt.Id}")).StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        var remaining = (await client.GetFromJsonAsync<HistoryItemDto[]>("/api/history?month=2026-04-01"))!.Single();
        await Assert.That(remaining.Receipt).IsNull();
        await Assert.That(remaining.Id).IsEqualTo(expense.Id);
        await Assert.That(remaining.Description).IsEqualTo(expense.Description);
        await Assert.That(remaining.Date).IsEqualTo(expense.Date);
        await Assert.That(remaining.Notes).IsEqualTo(expense.Notes);
        await Assert.That(remaining.Details.Single().Amount).IsEqualTo(-1300);
        await Assert.That(remaining.Details.Single().ExpenseCategoryId).IsEqualTo(categoryId);
        await Assert.That(remaining.Details.Single().Id).IsEqualTo(expense.Details.Single().Id);
        await Assert.That(factory.MonthNotifier.Months).IsEquivalentTo([new DateTime(2026, 3, 1), new DateTime(2026, 4, 1)]);
        await Assert.That(factory.ImageStore.Blobs.Count).IsEqualTo(0);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BudgetWebContext>();
        await Assert.That(await verificationDb.ExpenseCategoryItems.CountAsync()).IsEqualTo(1);
        await Assert.That(await verificationDb.ExpenseCategoryItemDetails.CountAsync()).IsEqualTo(1);
        await Assert.That(await verificationDb.ExpenseCategories.CountAsync()).IsEqualTo(1);
        await Assert.That(await verificationDb.ReceiptExpenseLinks.CountAsync()).IsEqualTo(0);
        await Assert.That(await verificationDb.Receipts.CountAsync()).IsEqualTo(0);
    }
}
