using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
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
            await fixture.PerformDatabaseOperation(async context =>
            {
                var category = new ExpenseCategory();
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
            });
        }
    }
}
