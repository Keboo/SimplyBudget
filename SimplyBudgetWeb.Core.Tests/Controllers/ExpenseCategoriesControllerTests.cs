using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class ExpenseCategoriesControllerTests
{
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
            Name: "New Name", CategoryName: "New Group", BudgetedAmount: 0, BudgetedPercentage: 0, Cap: null, AccountId: null));

        await Assert.That(result.Value!.Name).IsEqualTo("New Name");
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
}
