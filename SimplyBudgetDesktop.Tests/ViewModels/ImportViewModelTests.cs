using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetSharedTests;

namespace SimplyBudgetDesktop.Tests.ViewModels;

[TestClass]
public class ImportViewModelTests
{
    [TestMethod]
    public async Task OnImport_WhenItemMatchesExistingTransaction_IsMarkedDone()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();

        using var context = factory.Create();
        var date = DateTime.Today.AddDays(-2);
        var category = new ExpenseCategory();
        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync();

        var item = new ExpenseCategoryItem
        {
            Date = date,
            Description = "Existing item",
            Details = new()
            {
                new ExpenseCategoryItemDetail
                {
                    Amount = -10_71,
                    ExpenseCategory = category
                }
            }
        };
        context.ExpenseCategoryItems.Add(item);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<ImportViewModel>();
        vm.CsvData = TestCSVData.SingleTransaction;

        await vm.ImportCommand.ExecuteAsync(null);

        ImportItem importItem = vm.ImportedRecords.Single();
        Assert.IsTrue(importItem.IsDone);
    }

    [TestMethod]
    public async Task OnImport_WhenItemMatchesExistingIncome_IsMarkedDone()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();

        using var context = factory.Create();
        var date = DateTime.Today.AddDays(-1);
        var category = new ExpenseCategory();
        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync();

        var item = new ExpenseCategoryItem
        {
            Date = date,
            Description = "Existing item",
            Details = new()
            {
                new ExpenseCategoryItemDetail
                {
                    Amount = 10_00,
                    ExpenseCategory = category
                },
                new ExpenseCategoryItemDetail
                {
                    Amount = 5_23,
                    ExpenseCategory = category
                }
            }
        };
        context.ExpenseCategoryItems.Add(item);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<ImportViewModel>();
        vm.CsvData = TestCSVData.SingleIncome;

        await vm.ImportCommand.ExecuteAsync(null);

        ImportItem importItem = vm.ImportedRecords.Single();
        Assert.IsTrue(importItem.IsDone);
    }

    private static class TestCSVData
    {
        private const string Header = @"""Transaction ID"",""Posting Date"",""Effective Date"",""Transaction Type"",""Amount"",""Check Number"",""Reference Number"",""Description"",""Transaction Category"",""Type"",""Balance"",""Memo"",""Extended Description""";

        public static string SingleTransaction => Header + Environment.NewLine + CreateTransaction(10.71m);
        public static string SingleIncome => Header + Environment.NewLine + CreateIncome(15.23m);

        private static string CreateTransaction(decimal amount)
        {
            var date = DateTime.Today.AddDays(-2);
            return $@"""{date.Year}{date.Month}{date.Day}123456,12345,123,123,123,123,123"",""{date:g}"",""{date:g}"",""Debit"",""-{amount:N5}"","""",""Ref Num"",""Test Description"",""Test Category"",""Debit Card"",""12345.67000"","""",""Extended Test Description""";
        }

        private static string CreateIncome(decimal amount)
        {
            var date = DateTime.Today.AddDays(-1);
            return $@"""{date.Year}{date.Month}{date.Day}123456,12345,123,123,123,123,123"",""{date:g}"",""{date:g}"",""Credit"",""{amount:N5}"","""",""Ref Num"",""Test Description"",""Test Category"",""Debit Card"",""12345.67000"","""",""Extended Test Description""";
        }
    }
}
