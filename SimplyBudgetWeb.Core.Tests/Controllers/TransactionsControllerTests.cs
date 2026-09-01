using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.AutoMock;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class TransactionsControllerTests
{
    [Test]
    public async Task AddTransaction_NotifiesChangedMonth()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();
        var notifier = new Mock<IBudgetMonthUpdateNotifier>();

        var categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries", CurrentBalance = 500_00 };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            return category.ID;
        });

        var monthDate = new DateTime(2026, 1, 10);
        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new TransactionsController(context, notifier.Object);
            await controller.AddTransaction(new TransactionRequest(
                Description: "Costco",
                Date: monthDate,
                Items: [new TransactionItemRequest(categoryId, 45_00)]));
        }

        notifier.Verify(
            x => x.NotifyMonthUpdated(monthDate, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AddTransaction_PersistsNotes()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        var categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries", CurrentBalance = 500_00 };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            return category.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new TransactionsController(context);
            var result = await controller.AddTransaction(new TransactionRequest(
                Description: "Costco",
                Date: new DateTime(2026, 1, 10),
                Items: [new TransactionItemRequest(categoryId, 45_00)],
                Notes: "  Membership renewal  "));

            await Assert.That(result).IsTypeOf<StatusCodeResult>();
            await Assert.That(((StatusCodeResult)result).StatusCode).IsEqualTo(201);
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var item = await context.ExpenseCategoryItems
                .Include(x => x.Details)
                .SingleAsync();
            await Assert.That(item.Notes).IsEqualTo("Membership renewal");
            await Assert.That(item.Details!.Single().Amount).IsEqualTo(-45_00);
        });
    }

    [Test]
    public async Task AddIncome_WithWhitespaceNotes_StoresNullNotes()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        var categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Refunds", CurrentBalance = 0 };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            return category.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new TransactionsController(context);
            await controller.AddIncome(new TransactionRequest(
                Description: "Refund",
                Date: new DateTime(2026, 1, 10),
                Items: [new TransactionItemRequest(categoryId, 20_00)],
                Notes: "   "));
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var item = await context.ExpenseCategoryItems.SingleAsync();
            await Assert.That(item.Notes).IsNull();
        });
    }
}
