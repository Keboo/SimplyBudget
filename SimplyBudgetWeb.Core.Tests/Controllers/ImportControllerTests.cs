using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class ImportControllerTests
{
    private const string StcuHeader = @"""Transaction ID"",""Posting Date"",""Effective Date"",""Transaction Type"",""Amount"",""Check Number"",""Reference Number"",""Description"",""Transaction Category"",""Type"",""Balance"",""Memo"",""Extended Description""";

    private static string BuildCsv(string row) => StcuHeader + Environment.NewLine + row;

    [Test]
    public async Task Parse_ParsesDebitAmountAndMarksItemCheckedWhenNotDuplicate()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var csv = BuildCsv(@"""1"",""11/23/2020"",""11/23/2020"",""Debit"",""-21.77000"","""",""1"",""Purchase GOOGLE Play"",""Entertainment"",""Debit Card"",""1"","""",""Purchase GOOGLE Play""");
        var result = await controller.Parse(new ImportRequest(csv));

        var items = ((OkObjectResult)result.Result!).Value as ImportItemDto[];
        await Assert.That(items!.Length).IsEqualTo(1);
        await Assert.That(items[0].Amount).IsEqualTo(21_77);
        await Assert.That(items[0].IsDebit).IsTrue();
        await Assert.That(items[0].IsChecked).IsTrue();
        await Assert.That(items[0].IsDuplicate).IsFalse();
    }

    [Test]
    public async Task Parse_ParsesCreditAmountAsPositive()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var csv = BuildCsv(@"""1"",""11/20/2020"",""11/20/2020"",""Credit"",""1234.56000"","""",""1"",""PAYROLL"","""",""ACH"",""1"","""",""PAYROLL""");
        var result = await controller.Parse(new ImportRequest(csv));

        var items = ((OkObjectResult)result.Result!).Value as ImportItemDto[];
        await Assert.That(items!.Length).IsEqualTo(1);
        await Assert.That(items[0].IsDebit).IsFalse();
        await Assert.That(items[0].Amount).IsEqualTo(123456);
        await Assert.That(items[0].IsChecked).IsTrue();
        await Assert.That(items[0].IsDuplicate).IsFalse();
    }

    [Test]
    public async Task Parse_WithEfundTransferDescription_FiltersOutTransfer()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var csv = BuildCsv(@"""1"",""11/23/2020"",""11/23/2020"",""Debit"",""-200.00000"","""",""1"",""efund transfer"","""",""Debit Card"",""1"","""",""efund transfer""");
        var result = await controller.Parse(new ImportRequest(csv));

        var items = ((OkObjectResult)result.Result!).Value as ImportItemDto[];
        await Assert.That(items!.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Save_CreatesPendingExpensesForCheckedItems()
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
                Name = "Costco rule",
                RuleRegex = "Costco",
                ExpenseCategoryID = category.ID
            });
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var items = new[]
        {
            new ImportItemDto(new DateTime(2026, 1, 1), "Costco", 45_00, true, true),
            new ImportItemDto(new DateTime(2026, 1, 2), "Already handled", 10_00, true, false),
        };

        var result = await controller.Save(items);

        await Assert.That(((StatusCodeResult)result).StatusCode).IsEqualTo(201);
        var saved = await context.PendingExpenses.ToListAsync();
        await Assert.That(saved.Count).IsEqualTo(1);
        await Assert.That(saved[0].Description).IsEqualTo("Costco");
        await Assert.That(saved[0].Amount).IsEqualTo(45_00);
        await Assert.That(saved[0].SuggestedCategoryId).IsEqualTo(categoryId);
    }

    [Test]
    public async Task Save_WithEarlierUncategorizedMatch_UsesLaterCategorizedRule()
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
                Name = "Uncategorized Costco rule",
                RuleRegex = "Costco",
                ExpenseCategoryID = null
            });
            context.ExpenseCategoryRules.Add(new ExpenseCategoryRule
            {
                Name = "Categorized Costco rule",
                RuleRegex = "Costco",
                ExpenseCategoryID = category.ID
            });
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var items = new[]
        {
            new ImportItemDto(new DateTime(2026, 1, 1), "Costco", 45_00, true, true),
        };

        var result = await controller.Save(items);

        await Assert.That(((StatusCodeResult)result).StatusCode).IsEqualTo(201);
        var saved = await context.PendingExpenses.SingleAsync();
        await Assert.That(saved.SuggestedCategoryId).IsEqualTo(categoryId);
    }

    [Test]
    public async Task Save_CreditItem_DoesNotApplySuggestedCategoryRule()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Income" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryRules.Add(new ExpenseCategoryRule
            {
                Name = "Payroll rule",
                RuleRegex = "PAYROLL",
                ExpenseCategoryID = category.ID
            });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var items = new[]
        {
            new ImportItemDto(new DateTime(2026, 1, 1), "PAYROLL", 1_000_00, false, true),
        };

        var result = await controller.Save(items);

        await Assert.That(((StatusCodeResult)result).StatusCode).IsEqualTo(201);
        var saved = await context.PendingExpenses.SingleAsync();
        await Assert.That(saved.IsDebit).IsFalse();
        await Assert.That(saved.SuggestedCategoryId).IsNull();
    }

    [Test]
    public async Task Save_WithNoItemsChecked_CreatesNoPendingExpenses()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var items = new[]
        {
            new ImportItemDto(new DateTime(2026, 1, 2), "Already handled", 10_00, true, false),
        };

        await controller.Save(items);

        await Assert.That(await context.PendingExpenses.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_WithMatchingExistingPendingExpense_FlagsAsDuplicate()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        var date = new DateTime(2020, 11, 23);
        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.Add(new PendingExpense
            {
                Date = date,
                Description = "Purchase GOOGLE Play",
                Amount = 21_77,
                IsDebit = true,
            });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var csv = BuildCsv(@"""1"",""11/23/2020"",""11/23/2020"",""Debit"",""-21.77000"","""",""1"",""Purchase GOOGLE Play"",""Entertainment"",""Debit Card"",""1"","""",""Purchase GOOGLE Play""");
        var result = await controller.Parse(new ImportRequest(csv));

        var items = ((OkObjectResult)result.Result!).Value as ImportItemDto[];
        await Assert.That(items!.Length).IsEqualTo(1);
        await Assert.That(items[0].IsDuplicate).IsTrue();
        await Assert.That(items[0].IsChecked).IsFalse();
    }

    [Test]
    public async Task Parse_WithMatchingExistingExpenseCategoryItem_FlagsAsDuplicate()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        var date = new DateTime(2020, 11, 23);
        await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Entertainment" };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();
            await context.AddTransaction("Purchase GOOGLE Play", date, (21_77, category.ID));
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var csv = BuildCsv(@"""1"",""11/23/2020"",""11/23/2020"",""Debit"",""-21.77000"","""",""1"",""Purchase GOOGLE Play"",""Entertainment"",""Debit Card"",""1"","""",""Purchase GOOGLE Play""");
        var result = await controller.Parse(new ImportRequest(csv));

        var items = ((OkObjectResult)result.Result!).Value as ImportItemDto[];
        await Assert.That(items!.Length).IsEqualTo(1);
        await Assert.That(items[0].IsDuplicate).IsTrue();
        await Assert.That(items[0].IsChecked).IsFalse();
    }

    [Test]
    public async Task Parse_WithNoMatchingExistingData_DoesNotFlagAsDuplicate()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new ImportController(context);

        var csv = BuildCsv(@"""1"",""11/23/2020"",""11/23/2020"",""Debit"",""-21.77000"","""",""1"",""Purchase GOOGLE Play"",""Entertainment"",""Debit Card"",""1"","""",""Purchase GOOGLE Play""");
        var result = await controller.Parse(new ImportRequest(csv));

        var items = ((OkObjectResult)result.Result!).Value as ImportItemDto[];
        await Assert.That(items!.Length).IsEqualTo(1);
        await Assert.That(items[0].IsDuplicate).IsFalse();
        await Assert.That(items[0].IsChecked).IsTrue();
    }
}
