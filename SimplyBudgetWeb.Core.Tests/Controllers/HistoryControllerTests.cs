using Moq.AutoMock;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class HistoryControllerTests
{
    [Test]
    public async Task GetAll_FiltersByAmount()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.AddRange(
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 10),
                    Description = "Small purchase",
                    Details = [new ExpenseCategoryItemDetail { Amount = -1234, ExpenseCategoryId = category.ID }],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 12),
                    Description = "Large purchase",
                    Details = [new ExpenseCategoryItemDetail { Amount = -2500, ExpenseCategoryId = category.ID }],
                });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new HistoryController(context);

        var result = await controller.GetAll(
            month: new DateTime(2026, 1, 1),
            search: "12.34",
            categoryId: null,
            accountId: null);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Description).IsEqualTo("Small purchase");
        await Assert.That(result[0].Details[0].Amount).IsEqualTo(-1234);
    }
}
