using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Data;
using SimplyBudgetShared.DataTransfer;

using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class DataPortabilityControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task Export_ReturnsJsonFileWithFullPayload()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var account = new Account { Name = "Checking", ValidatedDate = new DateTime(2026, 6, 1) };
            context.Accounts.Add(account);
            await context.SaveChangesAsync();
            account = await context.SetAsDefaultAsync(account);
            await context.SaveChangesAsync();

            var category = new ExpenseCategory
            {
                Name = "Groceries",
                Description = "Food and household essentials",
                CategoryName = "Food",
                AccountID = account.ID,
                BudgetedAmount = 500_00,
                BudgetHistory =
                [
                    new ExpenseCategoryBudget { EffectiveMonth = DateTime.MinValue, BudgetedAmount = 400_00 },
                    new ExpenseCategoryBudget { EffectiveMonth = new DateTime(2026, 6, 1), BudgetedAmount = 500_00 },
                ]
            };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            await context.AddIncome("Paycheck", new DateTime(2026, 6, 2), (1_000_00, category.ID));

            context.ExpenseCategoryRules.Add(new ExpenseCategoryRule
            {
                Name = "Rule 1",
                RuleRegex = "PAYCHECK",
                ExpenseCategoryID = category.ID
            });
            context.Metadatas.Add(new Metadata { Key = "Version", Value = "1" });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new DataPortabilityController(context);

        IActionResult actionResult = await controller.Export(CancellationToken.None);
        if (actionResult is not FileContentResult fileResult)
        {
            throw new Exception($"Expected {nameof(FileContentResult)} but got {actionResult.GetType().Name}.");
        }

        if (fileResult.ContentType != "application/json")
        {
            throw new Exception($"Unexpected content type: '{fileResult.ContentType}'.");
        }

        if (string.IsNullOrWhiteSpace(fileResult.FileDownloadName) ||
            !fileResult.FileDownloadName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Expected export to have a .json download file name.");
        }

        var payload = JsonSerializer.Deserialize<BudgetDataExportPackage>(fileResult.FileContents, JsonOptions)
            ?? throw new Exception("Failed to deserialize export payload.");

        if (payload.Source != "web")
        {
            throw new Exception($"Expected source 'web', got '{payload.Source}'.");
        }

        if (payload.Accounts.Count != 1 ||
            payload.Categories.Count != 1 ||
            payload.Items.Count != 1 ||
            payload.ItemDetails.Count != 1 ||
            payload.Rules.Count != 1 ||
            payload.Metadata.Count != 1)
        {
            throw new Exception("Export payload did not include all expected data sets.");
        }

        await Assert.That(payload.FormatVersion).IsEqualTo(3);
        await Assert.That(string.Join(",", payload.Categories.Single().BudgetHistory!
            .Select(x => $"{x.EffectiveMonth:yyyy-MM-dd}:{x.BudgetedAmount}")))
            .IsEqualTo("0001-01-01:40000,2026-06-01:50000");
    }

    [Test]
    public async Task Import_WithNullPayload_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new DataPortabilityController(context);

        IActionResult actionResult = await controller.Import(null, CancellationToken.None);
        if (actionResult is not BadRequestObjectResult)
        {
            throw new Exception($"Expected {nameof(BadRequestObjectResult)} but got {actionResult.GetType().Name}.");
        }
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Import_LegacyJsonUsesExistingBudgetAsBaseline(int formatVersion)
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();
        var payload = JsonSerializer.Deserialize<BudgetDataExportPackage>(
            $$"""
            {
              "formatVersion": {{formatVersion}},
              "categories": [{ "id": 77, "name": "Legacy", "budgetedAmount": 12300 }]
            }
            """, JsonOptions)!;

        using var context = mocker.Get<BudgetWebContext>();
        var result = await new DataPortabilityController(context).Import(payload, CancellationToken.None);
        await Assert.That(result).IsTypeOf<NoContentResult>();
        var category = await context.ExpenseCategories.SingleAsync();
        await Assert.That(await context.ExpenseCategoryBudgets.CountAsync()).IsEqualTo(0);
        var graph = (await new ExpenseCategoriesController(context)
            .GetMonthlyExpenses(category.ID, new DateTime(2026, 4, 1), 2)).Value!;
        await Assert.That(graph.Months.All(x => x.BudgetedAmount == 12300)).IsTrue();
    }

    [Test]
    public async Task Import_ReplacesExistingDataAndReturnsNoContent()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.Accounts.Add(new Account { Name = "Old Account", ValidatedDate = DateTime.Today });
            await context.SaveChangesAsync();
        });

        var payload = new BudgetDataExportPackage
        {
            Source = "desktop",
            Accounts =
            [
                new BudgetDataExportAccount(1, "Checking", new DateTime(2026, 4, 1), false),
                new BudgetDataExportAccount(2, "Savings", new DateTime(2026, 4, 2), true)
            ],
            Categories =
            [
                new BudgetDataExportCategory(10, "Groceries", "Food and household essentials", "Food", 1, 500_00, 0, 125_00, null, false,
                [
                    new BudgetDataExportCategoryBudget(DateTime.MinValue, 400_00, 0),
                    new BudgetDataExportCategoryBudget(new DateTime(2026, 4, 1), 500_00, 0),
                ]),
                new BudgetDataExportCategory(20, "Emergency Fund", "Unexpected expenses", "Savings", 2, 0, 10, 875_00, null, false)
            ],
            Items =
            [
                new BudgetDataExportItem(100, new DateTime(2026, 4, 5), "Paycheck")
            ],
            ItemDetails =
            [
                new BudgetDataExportItemDetail(1000, 100, 10, 1_000_00, false),
                new BudgetDataExportItemDetail(2000, 100, 20, 500_00, false)
            ],
            Rules =
            [
                new BudgetDataExportRule(3000, "Paycheck rule", "PAYCHECK", 10)
            ],
            Metadata =
            [
                new BudgetDataExportMetadata(4000, "Version", "1")
            ]
        };

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new DataPortabilityController(context);
            IActionResult result = await controller.Import(payload, CancellationToken.None);
            if (result is not NoContentResult)
            {
                throw new Exception($"Expected {nameof(NoContentResult)} but got {result.GetType().Name}.");
            }
        }

        await mocker.InDbScopeAsync(async context =>
        {
            if (await context.Accounts.CountAsync() != 2)
            {
                throw new Exception("Expected imported accounts to replace existing accounts.");
            }

            if (await context.Accounts.AnyAsync(x => x.Name == "Old Account"))
            {
                throw new Exception("Expected previous account data to be removed.");
            }

            var defaultAccount = await context.Accounts.SingleOrDefaultAsync(x => x.IsDefault);
            if (defaultAccount?.Name != "Savings")
            {
                throw new Exception("Expected imported default account to be preserved.");
            }

            var categories = await context.ExpenseCategories.OrderBy(x => x.Name).ToListAsync();
            if (categories.Count != 2)
            {
                throw new Exception("Expected imported categories to be present.");
            }

            var groceries = categories.Single(x => x.Name == "Groceries");
            var emergency = categories.Single(x => x.Name == "Emergency Fund");
            var history = await context.ExpenseCategoryBudgets
                .Where(x => x.ExpenseCategoryId == groceries.ID)
                .OrderBy(x => x.EffectiveMonth)
                .ToListAsync();
            await Assert.That(string.Join(",", history.Select(x => $"{x.EffectiveMonth:yyyy-MM-dd}:{x.BudgetedAmount}")))
                .IsEqualTo("0001-01-01:40000,2026-04-01:50000");
            await Assert.That(await context.ExpenseCategoryBudgets.AnyAsync(x => x.ExpenseCategoryId == emergency.ID)).IsFalse();
            var legacyGraph = (await new ExpenseCategoriesController(context)
                .GetMonthlyExpenses(emergency.ID, new DateTime(2026, 4, 1), 2)).Value!;
            await Assert.That(legacyGraph.Months.All(x => x.BudgetedPercentage == 10)).IsTrue();
            if (groceries.Description != "Food and household essentials" ||
                emergency.Description != "Unexpected expenses")
            {
                throw new Exception("Category descriptions were not imported.");
            }

            if (groceries.CurrentBalance != 125_00 || emergency.CurrentBalance != 875_00)
            {
                throw new Exception("Expected category balances to match imported export values.");
            }

            if (await context.ExpenseCategoryRules.CountAsync() != 1 ||
                await context.Metadatas.CountAsync() != 1)
            {
                throw new Exception("Expected rules and metadata to be imported.");
            }
        });
    }
}