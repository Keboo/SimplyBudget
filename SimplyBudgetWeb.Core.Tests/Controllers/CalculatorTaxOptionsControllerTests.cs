using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class CalculatorTaxOptionsControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task Get_WithNoSavedOptions_ReturnsDefaultTaxOption()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new CalculatorTaxOptionsController(context);

        var result = await controller.Get();

        await Assert.That(result.Options.Count).IsEqualTo(1);
        await Assert.That(result.Options[0].Name).IsEqualTo("Tax");
        await Assert.That(result.Options[0].Percentage).IsEqualTo(9.1m);
        await Assert.That(result.Options[0].IsDefault).IsFalse();
    }

    [Test]
    public async Task Update_PersistsAndReturnsNormalizedOptions()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new CalculatorTaxOptionsController(context);
            var result = await controller.Update(new UpdateCalculatorTaxOptionsRequest(
            [
                new CalculatorTaxOptionDto("  State Tax  ", 6.2501m, false),
                new CalculatorTaxOptionDto("County Tax", 2.5m, true),
            ]));

            await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
            var dto = (result.Result as OkObjectResult)?.Value as CalculatorTaxOptionsDto;
            await Assert.That(dto).IsNotNull();
            await Assert.That(dto!.Options.Count).IsEqualTo(2);
            await Assert.That(dto.Options[0].Name).IsEqualTo("State Tax");
            await Assert.That(dto.Options[0].Percentage).IsEqualTo(6.25m);
            await Assert.That(dto.Options[1].IsDefault).IsTrue();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var saved = await context.Metadatas
                .AsNoTracking()
                .SingleAsync(x => x.Key == Metadata.CALCULATOR_TAX_OPTIONS_KEY);

            var options = JsonSerializer.Deserialize<IReadOnlyList<CalculatorTaxOptionDto>>(saved.Value!, JsonOptions);
            await Assert.That(options).IsNotNull();
            await Assert.That(options!.Count).IsEqualTo(2);
            await Assert.That(options[0].Name).IsEqualTo("State Tax");
            await Assert.That(options[1].IsDefault).IsTrue();
        });
    }

    [Test]
    public async Task Update_WithMultipleDefaults_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new CalculatorTaxOptionsController(context);

        var result = await controller.Update(new UpdateCalculatorTaxOptionsRequest(
        [
            new CalculatorTaxOptionDto("State Tax", 6m, true),
            new CalculatorTaxOptionDto("County Tax", 2m, true),
        ]));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task Update_WithDuplicateNames_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new CalculatorTaxOptionsController(context);

        var result = await controller.Update(new UpdateCalculatorTaxOptionsRequest(
        [
            new CalculatorTaxOptionDto("Tax", 6m, false),
            new CalculatorTaxOptionDto(" tax ", 5m, false),
        ]));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
    }
}
