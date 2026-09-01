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

    [Test]
    public async Task GetAll_ReturnsSelectedMonthItemsOrderedByOldestDateFirst()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Food" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.AddRange(
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 6, 20),
                    Description = "June newest",
                    Details =
                    [
                        new ExpenseCategoryItemDetail { ExpenseCategoryId = category.ID, Amount = -1200 },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 6, 3),
                    Description = "June oldest",
                    Details =
                    [
                        new ExpenseCategoryItemDetail { ExpenseCategoryId = category.ID, Amount = -800 },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 6, 12),
                    Description = "June middle",
                    Details =
                    [
                        new ExpenseCategoryItemDetail { ExpenseCategoryId = category.ID, Amount = -1000 },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 7, 1),
                    Description = "July item",
                    Details =
                    [
                        new ExpenseCategoryItemDetail { ExpenseCategoryId = category.ID, Amount = -500 },
                    ],
                });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new HistoryController(context);

        var result = await controller.GetAll(
            month: new DateTime(2026, 6, 1),
            search: null,
            categoryId: null,
            accountId: null);

        await Assert.That(result.Length).IsEqualTo(3);
        await Assert.That(result[0].Description).IsEqualTo("June oldest");
        await Assert.That(result[1].Description).IsEqualTo("June middle");
        await Assert.That(result[2].Description).IsEqualTo("June newest");
    }

    [Test]
    public async Task GetAll_ReturnsAndSearchesNotes()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.Add(new ExpenseCategoryItem
            {
                Date = new DateTime(2026, 1, 10),
                Description = "Costco",
                Notes = "Business membership renewal",
                Details = [new ExpenseCategoryItemDetail { Amount = -1234, ExpenseCategoryId = category.ID }],
            });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new HistoryController(context);

        var result = await controller.GetAll(
            month: new DateTime(2026, 1, 1),
            search: "membership",
            categoryId: null,
            accountId: null);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Notes).IsEqualTo("Business membership renewal");
    }
}
