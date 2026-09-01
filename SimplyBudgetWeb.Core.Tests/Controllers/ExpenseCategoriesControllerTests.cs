using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class ExpenseCategoriesControllerTests
{
    [Test]
    public async Task GetAll_FiltersByAmount()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.ExpenseCategories.AddRange(
                new ExpenseCategory { Name = "Groceries", BudgetedAmount = 1234 },
                new ExpenseCategory { Name = "Savings", BudgetedAmount = 3000 });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.GetAll(includeHidden: true, search: "12.34");

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("Groceries");
        await Assert.That(result[0].BudgetedAmount).IsEqualTo(1234);
    }

    [Test]
    public async Task GetAll_FiltersByDescription()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.ExpenseCategories.AddRange(
                new ExpenseCategory { Name = "Groceries", Description = "Food and household essentials" },
                new ExpenseCategory { Name = "Savings", Description = "Long-term goals" });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.GetAll(includeHidden: true, search: "household");

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("Groceries");
        await Assert.That(result[0].Description).IsEqualTo("Food and household essentials");
    }

    [Test]
    public async Task Update_RenamesCategory()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Old Name", CategoryName = "Old Group" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.Update(categoryId, new ExpenseCategoryRequest(
            Name: "New Name", Description: "New Description", CategoryName: "New Group",
            BudgetedAmount: 0, BudgetedPercentage: 0, Cap: null, AccountId: null));

        await Assert.That(result.Value!.Name).IsEqualTo("New Name");
        await Assert.That(result.Value!.Description).IsEqualTo("New Description");
        await Assert.That(result.Value!.CategoryName).IsEqualTo("New Group");
    }

    [Test]
    public async Task Hide_SetsIsHidden()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.Hide(categoryId);

        await Assert.That(result.Value!.IsHidden).IsTrue();
    }

    [Test]
    public async Task Restore_ClearsIsHidden()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries", IsHidden = true };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.Restore(categoryId);

        await Assert.That(result.Value!.IsHidden).IsFalse();
    }

    [Test]
    public async Task Delete_RemovesCategory_WhenItHasNoItems()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Unused" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            return category.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new ExpenseCategoriesController(context);
            var result = await controller.Delete(categoryId);
            await Assert.That(result).IsTypeOf<NoContentResult>();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var count = await context.ExpenseCategories.CountAsync(c => c.ID == categoryId);
            await Assert.That(count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task Delete_ReturnsConflict_WhenCategoryHasItems()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.Add(new ExpenseCategoryItem
            {
                Date = DateTime.Today,
                Description = "Store run",
                Details =
                [
                    new ExpenseCategoryItemDetail { Amount = -500, ExpenseCategoryId = category.ID }
                ],
            });
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.Delete(categoryId);

        await Assert.That(result).IsTypeOf<ConflictObjectResult>();

        var stored = await context.ExpenseCategories.FindAsync(categoryId);
        await Assert.That(stored).IsNotNull();
    }

    [Test]
    public async Task Delete_ReturnsConflict_WhenCategoryIsReferencedByARule()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Groceries" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryRules.Add(new ExpenseCategoryRule
            {
                Name = "Grocery rule",
                RuleRegex = "grocery",
                ExpenseCategoryID = category.ID,
            });
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.Delete(categoryId);

        await Assert.That(result).IsTypeOf<ConflictObjectResult>();
    }

    [Test]
    public async Task GetAll_MarksHasItems_ForCategoriesWithPostedItems()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        var (withItemsId, withoutItemsId) = await mocker.InDbScopeAsync(async context =>
        {
            var withItems = new ExpenseCategory { Name = "Has Items" };
            var withoutItems = new ExpenseCategory { Name = "No Items" };
            context.ExpenseCategories.AddRange(withItems, withoutItems);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.Add(new ExpenseCategoryItem
            {
                Date = DateTime.Today,
                Description = "Store run",
                Details =
                [
                    new ExpenseCategoryItemDetail { Amount = -500, ExpenseCategoryId = withItems.ID }
                ],
            });
            await context.SaveChangesAsync();
            return (withItems.ID, withoutItems.ID);
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.GetAll(includeHidden: false, search: null);

        await Assert.That(result.Single(c => c.Id == withItemsId).HasItems).IsTrue();
        await Assert.That(result.Single(c => c.Id == withoutItemsId).HasItems).IsFalse();
    }

    [Test]
    public async Task GetRemainingBudget_ReturnsRemainingAmountPerCategory()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        var (groceriesId, savingsId) = await mocker.InDbScopeAsync(async context =>
        {
            var groceries = new ExpenseCategory { Name = "Groceries", BudgetedAmount = 30000 };
            var savings = new ExpenseCategory { Name = "Savings", BudgetedPercentage = 10 };
            context.ExpenseCategories.AddRange(groceries, savings);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.Add(new ExpenseCategoryItem
            {
                Date = new DateTime(2026, 1, 10),
                Description = "Paycheck allocation",
                Details = [new ExpenseCategoryItemDetail { Amount = 10000, ExpenseCategoryId = groceries.ID }],
            });
            await context.SaveChangesAsync();
            return (groceries.ID, savings.ID);
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.GetRemainingBudget(new DateTime(2026, 1, 15));

        await Assert.That(result[groceriesId].CurrentAmount).IsEqualTo(10000);
        await Assert.That(result[groceriesId].RemainingAmount).IsEqualTo(20000);
        await Assert.That(result[savingsId].CurrentAmount).IsEqualTo(0);
        await Assert.That(result[savingsId].RemainingAmount).IsEqualTo(0);
    }

    [Test]
    public async Task GetMonthlyExpenses_ReturnsMonthlySpendingAndBudgetedAmount()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var groceries = new ExpenseCategory { Name = "Groceries", BudgetedAmount = 30000 };
            var savings = new ExpenseCategory { Name = "Savings" };
            context.ExpenseCategories.AddRange(groceries, savings);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.AddRange(
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 10),
                    Description = "Store run",
                    Details = [new ExpenseCategoryItemDetail { Amount = -8000, ExpenseCategoryId = groceries.ID }],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 15),
                    Description = "Ignored purchase",
                    Details = [new ExpenseCategoryItemDetail { Amount = -2000, ExpenseCategoryId = groceries.ID, IgnoreBudget = true }],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 2, 12),
                    Description = "Transfer to savings",
                    Details =
                    [
                        new ExpenseCategoryItemDetail { Amount = -1000, ExpenseCategoryId = groceries.ID },
                        new ExpenseCategoryItemDetail { Amount = 1000, ExpenseCategoryId = savings.ID },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 3, 5),
                    Description = "Another store run",
                    Details = [new ExpenseCategoryItemDetail { Amount = -5000, ExpenseCategoryId = groceries.ID }],
                });
            await context.SaveChangesAsync();

            return groceries.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExpenseCategoriesController(context);

        var result = await controller.GetMonthlyExpenses(categoryId, new DateTime(2026, 3, 1), months: 3);

        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.BudgetedAmount).IsEqualTo(30000);
        await Assert.That(result.Value.Months.Length).IsEqualTo(3);
        await Assert.That(result.Value.Months[0].Month).IsEqualTo("2026-01");
        await Assert.That(result.Value.Months[0].Amount).IsEqualTo(8000);
        await Assert.That(result.Value.Months[1].Month).IsEqualTo("2026-02");
        await Assert.That(result.Value.Months[1].Amount).IsEqualTo(0);
        await Assert.That(result.Value.Months[2].Month).IsEqualTo("2026-03");
        await Assert.That(result.Value.Months[2].Amount).IsEqualTo(5000);
    }
}
