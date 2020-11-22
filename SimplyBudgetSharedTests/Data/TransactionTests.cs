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
                var item = await context.FindAsync<ExpenseCategoryItem>(transaction!.ID);
                context.Remove(item);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.IsFalse(await context.ExpenseCategoryItems.AnyAsync());
                Assert.IsFalse(await context.ExpenseCategoryItemDetails.AnyAsync());
                Assert.AreEqual(250, (await context.FindAsync<ExpenseCategory>(category.ID)).CurrentBalance);
            });
        }

        [TestMethod]
        public async Task OnRetrieve_GetsRelatedItems()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            var category = new ExpenseCategory { CurrentBalance = 250 };
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(category);
                await context.SaveChangesAsync();
                await context.AddTransaction("Test", DateTime.Now, (100, category.ID));
            });

            //Act
            ExpenseCategoryItem? transaction = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                transaction = await context.ExpenseCategoryItems
                    .Include(x => x.Details)
                    .ThenInclude(x => x.ExpenseCategory)
                    .SingleAsync();
            });

            //Assert
            Assert.IsNotNull(transaction?.Details);
            var item = transaction!.Details.Single();
            Assert.AreEqual(-100, item.Amount);
            Assert.AreEqual(category.ID, item.ExpenseCategory?.ID);
        }
    }
}
