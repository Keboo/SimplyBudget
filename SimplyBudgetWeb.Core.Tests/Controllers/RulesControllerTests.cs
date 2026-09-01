using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class RulesControllerTests
{
    [Test]
    public async Task Create_PersistsAmountRange()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new RulesController(context);
            var result = await controller.Create(new RuleRequest(
                "Costco",
                "Costco",
                ExpenseCategoryId: null,
                Notes: null,
                MinimumAmount: 10_00,
                MaximumAmount: 50_00));

            await Assert.That(result.Result).IsTypeOf<CreatedAtActionResult>();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var rule = await context.ExpenseCategoryRules.SingleAsync();
            await Assert.That(rule.MinimumAmount).IsEqualTo(10_00);
            await Assert.That(rule.MaximumAmount).IsEqualTo(50_00);
        });
    }

    [Test]
    public async Task Create_WithMinimumGreaterThanMaximum_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new RulesController(context);

        var result = await controller.Create(new RuleRequest(
            "Costco",
            "Costco",
            ExpenseCategoryId: null,
            Notes: null,
            MinimumAmount: 50_00,
            MaximumAmount: 10_00));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
        await Assert.That(await context.ExpenseCategoryRules.AnyAsync()).IsFalse();
    }

    [Test]
    public async Task Create_WithNegativeAmount_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new RulesController(context);

        var result = await controller.Create(new RuleRequest(
            "Costco",
            "Costco",
            ExpenseCategoryId: null,
            Notes: null,
            MinimumAmount: -1));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task Update_ChangesAmountRange()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int ruleId = await mocker.InDbScopeAsync(async context =>
        {
            var rule = new ExpenseCategoryRule
            {
                Name = "Costco",
                RuleRegex = "Costco",
                MinimumAmount = 10_00,
                MaximumAmount = 50_00
            };
            context.ExpenseCategoryRules.Add(rule);
            await context.SaveChangesAsync();
            return rule.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new RulesController(context);
            var result = await controller.Update(ruleId, new RuleRequest(
                "Costco",
                "Costco",
                ExpenseCategoryId: null,
                Notes: null,
                MinimumAmount: null,
                MaximumAmount: 100_00));

            await Assert.That(result.Value!.MinimumAmount).IsNull();
            await Assert.That(result.Value!.MaximumAmount).IsEqualTo(100_00);
        }
    }

    [Test]
    public async Task Update_WithMinimumGreaterThanMaximum_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int ruleId = await mocker.InDbScopeAsync(async context =>
        {
            var rule = new ExpenseCategoryRule { Name = "Costco", RuleRegex = "Costco" };
            context.ExpenseCategoryRules.Add(rule);
            await context.SaveChangesAsync();
            return rule.ID;
        });

        using var updateContext = mocker.Get<BudgetWebContext>();
        var updateController = new RulesController(updateContext);

        var badResult = await updateController.Update(ruleId, new RuleRequest(
            "Costco",
            "Costco",
            ExpenseCategoryId: null,
            Notes: null,
            MinimumAmount: 50_00,
            MaximumAmount: 10_00));

        await Assert.That(badResult.Result).IsTypeOf<BadRequestObjectResult>();
    }
}
