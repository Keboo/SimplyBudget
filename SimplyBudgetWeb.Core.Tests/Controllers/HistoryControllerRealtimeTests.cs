using Microsoft.AspNetCore.Mvc;

using Moq;
using Moq.AutoMock;

using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class HistoryControllerRealtimeTests
{
    [Test]
    public async Task Delete_NotifiesChangedMonth()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();
        var notifier = new Mock<IBudgetMonthUpdateNotifier>();

        var monthDate = new DateTime(2026, 5, 19);
        int itemId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Home" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            var item = new ExpenseCategoryItem
            {
                Date = monthDate,
                Description = "Home Depot",
                Details =
                [
                    new ExpenseCategoryItemDetail
                    {
                        ExpenseCategoryId = category.ID,
                        Amount = -1200,
                    },
                ],
            };
            context.ExpenseCategoryItems.Add(item);
            await context.SaveChangesAsync();
            return item.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new HistoryController(context, notifier.Object);

        var result = await controller.Delete(itemId);

        await Assert.That(result).IsTypeOf<NoContentResult>();
        notifier.Verify(
            x => x.NotifyMonthUpdated(monthDate, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
