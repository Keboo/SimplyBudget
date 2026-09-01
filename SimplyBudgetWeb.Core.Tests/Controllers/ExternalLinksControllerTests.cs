using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Data;

using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class ExternalLinksControllerTests
{
    [Test]
    public async Task GetAll_ReturnsRulesOrderedByName()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.ExternalLinkRules.AddRange(
                new ExternalLinkRule { Name = "Target", RuleRegex = "target", Url = "https://www.target.com/orders" },
                new ExternalLinkRule { Name = "Amazon", RuleRegex = @"\bamazon\b", Url = "https://www.amazon.com/cpe/yourpayments/transactions" });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExternalLinksController(context);

        var result = await controller.GetAll();

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("Amazon");
        await Assert.That(result[0].Url).IsEqualTo("https://www.amazon.com/cpe/yourpayments/transactions");
        await Assert.That(result[1].Name).IsEqualTo("Target");
    }

    [Test]
    public async Task Create_AddsRule()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new ExternalLinksController(context);
            var result = await controller.Create(new ExternalLinkRuleRequest("Costco", "costco", "https://www.costco.com/OrderStatusView"));
            await Assert.That(result.Result).IsTypeOf<CreatedAtActionResult>();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var rule = await context.ExternalLinkRules.SingleAsync();
            await Assert.That(rule.Name).IsEqualTo("Costco");
            await Assert.That(rule.RuleRegex).IsEqualTo("costco");
            await Assert.That(rule.Url).IsEqualTo("https://www.costco.com/OrderStatusView");
        });
    }

    [Test]
    public async Task Create_WithInvalidRegex_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExternalLinksController(context);

        var result = await controller.Create(new ExternalLinkRuleRequest("Bad", "[unclosed", "https://example.com"));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
        await Assert.That(await context.ExternalLinkRules.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Create_WithNonHttpUrl_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExternalLinksController(context);

        var result = await controller.Create(new ExternalLinkRuleRequest("Bad", "amazon", "javascript:alert(1)"));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
        await Assert.That(await context.ExternalLinkRules.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Update_ModifiesRule()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int ruleId = await mocker.InDbScopeAsync(async context =>
        {
            var rule = new ExternalLinkRule { Name = "Amazon", RuleRegex = "amazon", Url = "https://www.amazon.com" };
            context.ExternalLinkRules.Add(rule);
            await context.SaveChangesAsync();
            return rule.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new ExternalLinksController(context);
            var result = await controller.Update(ruleId, new ExternalLinkRuleRequest("Amazon Orders", @"\bamazon\b", "https://www.amazon.com/gp/css/order-history"));
            await Assert.That(result.Value?.Name).IsEqualTo("Amazon Orders");
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var rule = await context.ExternalLinkRules.SingleAsync();
            await Assert.That(rule.RuleRegex).IsEqualTo(@"\bamazon\b");
            await Assert.That(rule.Url).IsEqualTo("https://www.amazon.com/gp/css/order-history");
        });
    }

    [Test]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ExternalLinksController(context);

        var result = await controller.Update(42, new ExternalLinkRuleRequest("Amazon", "amazon", "https://www.amazon.com"));

        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task Delete_RemovesRule()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int ruleId = await mocker.InDbScopeAsync(async context =>
        {
            var rule = new ExternalLinkRule { Name = "Amazon", RuleRegex = "amazon", Url = "https://www.amazon.com" };
            context.ExternalLinkRules.Add(rule);
            await context.SaveChangesAsync();
            return rule.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new ExternalLinksController(context);
            var result = await controller.Delete(ruleId);
            await Assert.That(result).IsTypeOf<NoContentResult>();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            await Assert.That(await context.ExternalLinkRules.CountAsync()).IsEqualTo(0);
        });
    }
}
