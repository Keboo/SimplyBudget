using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class ImportViewModelTests
    {
        [TestMethod]
        public async Task OnImport_WhenItemMatchesExcisting_IsMarkedDone()
        {
            var mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.WithDbScope();

            var context = mocker.Get<BudgetContext>();
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

        private static class TestCSVData
        {
            private const string Header = @"""Transaction ID"",""Posting Date"",""Effective Date"",""Transaction Type"",""Amount"",""Check Number"",""Reference Number"",""Description"",""Transaction Category"",""Type"",""Balance"",""Memo"",""Extended Description""";

            public static string SingleTransaction => Header + Environment.NewLine + CreateTransaction(10.71m);

            private static string CreateTransaction(decimal amount)
            {
                var date = DateTime.Today.AddDays(-2);
                return $@"""{date.Year}{date.Month}{date.Day}123456,12345,123,123,123,123,123"",""{date:g}"",""{date:g}"",""Debit"",""-{amount:N5}"","""",""Ref Num"",""Test Description"",""Test Category"",""Debit Card"",""12345.67000"","""",""Extended Test Descriptio""";
            }
        }
    }
}
