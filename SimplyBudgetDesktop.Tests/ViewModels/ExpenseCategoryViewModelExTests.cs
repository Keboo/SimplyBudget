using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetSharedTests.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class ExpenseCategoryViewModelExTests
    {
        [TestMethod]
        public async Task Create_SpecifyingMonth_LoadsData()
        {
            var fixture = new BudgetDatabaseContext();
            var expenseCategory = new ExpenseCategory();
            var now = DateTime.Now;

            var twoMonthsAgo = now.AddMonths(-2);
            var lastMonth = now.AddMonths(-1);

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(expenseCategory);
                await context.SaveChangesAsync();
                
                await context.AddTransaction(expenseCategory.ID, 100, "Transaction 1", twoMonthsAgo);
                await context.AddIncome("Income 1", twoMonthsAgo, (300, expenseCategory.ID));
                
                await context.AddTransaction(expenseCategory.ID, 200, "Transaction 2", lastMonth);
                await context.AddIncome("Income 2", lastMonth, (300, expenseCategory.ID));

                await context.AddTransaction(expenseCategory.ID, 300, "Transaction 3", now);
                await context.AddIncome("Income 3", now, (300, expenseCategory.ID));
            });

            await fixture.PerformDatabaseOperation(async context =>
            {
                var vm = await ExpenseCategoryViewModelEx.Create(context, expenseCategory, lastMonth);

                // Balance always shows current even if we query a previous month
                Assert.AreEqual(300, vm.Balance);
                Assert.AreEqual(300, vm.MonthlyAllocations);
                Assert.AreEqual(200, vm.MonthlyExpenses);
            });
        }
    }
}
