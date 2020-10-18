using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class TransactionTests
    {
        [TestMethod]
        public async Task OnRemove_RemovesChildren()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            Transaction? transaction = null;
            var category = new ExpenseCategory { CurrentBalance = 250 };
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(category);
                await context.SaveChangesAsync();
                transaction = await context.AddTransaction("Test", DateTime.Now, (100, category.ID));
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                transaction = await context.FindAsync<Transaction>(transaction!.ID);
                context.Remove(transaction);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.IsFalse(await context.Transactions.AnyAsync());
                Assert.IsFalse(await context.TransactionItems.AnyAsync());
                Assert.AreEqual(250, (await context.FindAsync<ExpenseCategory>(category.ID)).CurrentBalance);
            });
        }

        [TestMethod]
        public async Task OnRetrieve_GetsRelatedItems()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            Transaction? transaction = null;
            var category = new ExpenseCategory { CurrentBalance = 250 };
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(category);
                await context.SaveChangesAsync();
                transaction = await context.AddTransaction("Test", DateTime.Now, (100, category.ID));
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                transaction = await context.Transactions
                    .Include(x => x.TransactionItems)
                    .ThenInclude(x => x.ExpenseCategory)
                    .SingleAsync();
            });

            //Assert
            Assert.IsNotNull(transaction?.TransactionItems);
            var item = transaction!.TransactionItems.Single();
            Assert.AreEqual(100, item.Amount);
            Assert.AreEqual(category.ID, item.ExpenseCategory?.ID);
        }
    }
}
