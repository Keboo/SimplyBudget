using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
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
                var income1 = new Income { Date = twoMonthsAgo };
                var income2 = new Income { Date = lastMonth };
                var income3 = new Income { Date = now };

                context.ExpenseCategories.Add(expenseCategory);
                context.Incomes.AddRange(income1, income2, income3);
                await context.SaveChangesAsync();
                
                await context.AddTransaction(expenseCategory, 100, "Transaction 1", twoMonthsAgo);
                await context.AddIncomeItem(expenseCategory, income1, 300);
                
                await context.AddTransaction(expenseCategory, 200, "Transaction 2", lastMonth);
                await context.AddIncomeItem(expenseCategory, income2, 300);

                await context.AddTransaction(expenseCategory, 300, "Transaction 3", now);
                await context.AddIncomeItem(expenseCategory, income3, 300);
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
