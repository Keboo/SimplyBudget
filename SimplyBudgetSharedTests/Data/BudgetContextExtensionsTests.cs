using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class BudgetContextExtensionsTests
    {
        [TestMethod]
        public async Task AddIncomeItem_UpdatesTotalAmount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            var expenseCategory = new ExpenseCategory { CurrentBalance = 20 };
            var income = new Income();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(expenseCategory);
                context.Add(income);
                await context.SaveChangesAsync();
            });


            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddIncomeItem(expenseCategory, income, 150);
            });


            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var incomeItem = await context.IncomeItems.SingleAsync(x => x.IncomeID == income.ID);
                Assert.AreEqual(150, incomeItem.Amount);
                var category = await context.ExpenseCategories.FindAsync(expenseCategory.ID);
                Assert.AreEqual(170, category.CurrentBalance);
            });
        }

        [TestMethod]
        public async Task AddTransaction_UpdatesTotalAmount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            var expenseCategory = new ExpenseCategory { CurrentBalance = 150 };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(expenseCategory);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddTransaction(expenseCategory.ID, 80, "transaction description");
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var transaction = await context.TransactionItems.SingleAsync(x => x.ExpenseCategoryID == expenseCategory.ID);
                Assert.AreEqual(80, transaction.Amount);
                var category = await context.ExpenseCategories.FindAsync(expenseCategory.ID);
                Assert.AreEqual(70, category.CurrentBalance);
            });
        }

        [TestMethod]
        public async Task AddTransfer_MovesAmount()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var category1 = new ExpenseCategory { CurrentBalance = 150 };
            var category2 = new ExpenseCategory { CurrentBalance = 150 };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category1, category2);
                await context.SaveChangesAsync();
            });
            var now = DateTime.Now;

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddTransfer("Test", now, 50, category1, category2);
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                category1 = await context.FindAsync<ExpenseCategory>(category1.ID);
                category2 = await context.FindAsync<ExpenseCategory>(category2.ID);

                Assert.AreEqual(100, category1.CurrentBalance);
                Assert.AreEqual(200, category2.CurrentBalance);

                var transfer = await context.Transfers.SingleAsync();
                Assert.AreEqual(now, transfer.Date);
                Assert.AreEqual(50, transfer.Amount);
                Assert.AreEqual("Test", transfer.Description);
                Assert.AreEqual(category1.ID, transfer.FromExpenseCategoryID);
                Assert.AreEqual(category2.ID, transfer.ToExpenseCategoryID);
            });
        }
    }
}
