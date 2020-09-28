using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
                await context.AddTransaction(expenseCategory, 80, "transaction description");
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
    }
}
