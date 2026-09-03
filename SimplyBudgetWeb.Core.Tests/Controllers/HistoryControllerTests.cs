using Moq.AutoMock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

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

    [Test]
    public async Task GetAll_ReturnsAndSearchesCategoryDescription()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries", Description = "Costco and Aldi runs" };
            var otherCategory = new ExpenseCategory { Name = "Utilities", Description = "Power and water" };
            context.ExpenseCategories.AddRange(category, otherCategory);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.AddRange(
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 10),
                    Description = "Weekly shopping",
                    Details = [new ExpenseCategoryItemDetail { Amount = -1234, ExpenseCategoryId = category.ID }],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 11),
                    Description = "Electric bill",
                    Details = [new ExpenseCategoryItemDetail { Amount = -5678, ExpenseCategoryId = otherCategory.ID }],
                });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new HistoryController(context);

        var result = await controller.GetAll(
            month: new DateTime(2026, 1, 1),
            search: "Aldi",
            categoryId: null,
            accountId: null);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Description).IsEqualTo("Weekly shopping");
        await Assert.That(result[0].Details[0].CategoryDescription).IsEqualTo("Costco and Aldi runs");
    }

    [Test]
    public async Task Update_UpdatesNotesAndReturnsUpdatedItem()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();
        var cache = new Mock<IBudgetMonthDataCache>();

        var (itemId, itemDate) = await mocker.InDbScopeAsync(async context =>
        {
            var date = new DateTime(2026, 1, 10);
            var category = new ExpenseCategory { Name = "Groceries" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            var item = new ExpenseCategoryItem
            {
                Date = date,
                Description = "Costco",
                Notes = "Original note",
                Details = [new ExpenseCategoryItemDetail { Amount = -1234, ExpenseCategoryId = category.ID }],
            };
            context.ExpenseCategoryItems.Add(item);
            await context.SaveChangesAsync();

            return (item.ID, date);
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new HistoryController(context, budgetMonthDataCache: cache.Object);
            var result = await controller.Update(itemId, new HistoryItemUpdateRequest("  Updated note  "));

            var dto = (result.Value as HistoryItemDto) ?? ((OkObjectResult?)result.Result)?.Value as HistoryItemDto;
            await Assert.That(dto).IsNotNull();
            await Assert.That(dto!.Notes).IsEqualTo("Updated note");
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var stored = await context.ExpenseCategoryItems.SingleAsync(x => x.ID == itemId);
            await Assert.That(stored.Notes).IsEqualTo("Updated note");
        });

        cache.Verify(x => x.InvalidateMonth(itemDate), Times.Once);
    }

    [Test]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new HistoryController(context);

        var result = await controller.Update(999, new HistoryItemUpdateRequest("Some note"));

        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }
}
