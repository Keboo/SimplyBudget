using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class ReceiptsControllerTests
{
    [Test]
    public async Task FindMatches_RequiresExactAmountWithinFiveDaysAndAnUnlinkedReceipt()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();
        var date = new DateTime(2026, 1, 15);
        int[] receiptIds = [];

        await mocker.InDbScopeAsync(async context =>
        {
            var candidates = new[]
            {
                new Receipt { FileName = "near.jpg", BlobName = "near.jpg", TransactionDate = date.AddDays(-1), TotalAmountCents = 1500 },
                new Receipt { FileName = "edge.jpg", BlobName = "edge.jpg", TransactionDate = date.AddDays(5), TotalAmountCents = 1500 },
                new Receipt { FileName = "outside.jpg", BlobName = "outside.jpg", TransactionDate = date.AddDays(-6), TotalAmountCents = 1500 },
                new Receipt { FileName = "amount.jpg", BlobName = "amount.jpg", TransactionDate = date, TotalAmountCents = 1501 },
                new Receipt { FileName = "linked.jpg", BlobName = "linked.jpg", TransactionDate = date, TotalAmountCents = 1500 },
            };
            context.Receipts.AddRange(candidates);
            await context.SaveChangesAsync();

            var expense = new ExpenseCategoryItem { Date = date, Description = "Store" };
            context.ExpenseCategoryItems.Add(expense);
            await context.SaveChangesAsync();
            context.ReceiptExpenseLinks.Add(new ReceiptExpenseLink
            {
                ReceiptId = candidates[4].Id,
                ExpenseCategoryItemId = expense.ID,
            });
            await context.SaveChangesAsync();

            receiptIds = candidates.Select(x => x.Id).ToArray();
        });

        using var db = mocker.Get<BudgetWebContext>();
        var controller = new ReceiptsController(
            db,
            Mock.Of<IReceiptImageStore>(),
            Mock.Of<IReceiptAnalyzer>(),
            NullLogger<ReceiptsController>.Instance);

        var result = await controller.FindMatches(1500, date);

        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Select(x => x.Id).ToArray())
            .IsEquivalentTo(new[] { receiptIds[0], receiptIds[1] });
    }

    [Test]
    public async Task Update_StoresUserCorrectionsAndOptionalLineItems()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();
        var receiptId = await mocker.InDbScopeAsync(async context =>
        {
            var receipt = new Receipt
            {
                FileName = "receipt.jpg",
                BlobName = "receipt.jpg",
                MerchantName = "Wrong location",
                TotalAmountCents = 1000,
            };
            context.Receipts.Add(receipt);
            await context.SaveChangesAsync();
            return receipt.Id;
        });

        using (var db = mocker.Get<BudgetWebContext>())
        {
            var controller = new ReceiptsController(
                db,
                Mock.Of<IReceiptImageStore>(),
                Mock.Of<IReceiptAnalyzer>(),
                NullLogger<ReceiptsController>.Instance);
            var result = await controller.Update(receiptId, new ReceiptUpdateRequest(
                MerchantName: "Correct location",
                TransactionDate: new DateTime(2026, 2, 1),
                TotalAmountCents: 1234,
                Notes: "Client lunch",
                LineItems: [new ReceiptLineItemUpdateRequest("Coffee", 400)]));

            await Assert.That(result.Value?.MerchantName).IsEqualTo("Correct location");
            await Assert.That(result.Value?.LineItems.Single().AmountCents).IsEqualTo(400);
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var saved = await context.Receipts.Include(x => x.LineItems).SingleAsync();
            await Assert.That(saved.TransactionDate).IsEqualTo(new DateTime(2026, 2, 1));
            await Assert.That(saved.TotalAmountCents).IsEqualTo(1234);
            await Assert.That(saved.Notes).IsEqualTo("Client lunch");
            await Assert.That(saved.LineItems.Single().Description).IsEqualTo("Coffee");
        });
    }
}
