using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class IncomeTests
    {
        [TestMethod]
        public async Task OnRemove_RemovesChildren()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();
            var category = new ExpenseCategory { CurrentBalance = 250 };

            Income? income = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(category);
                await context.SaveChangesAsync();
                income = await context.AddIncome("Test", DateTime.Now, (100, category.ID));
            });

            //Act
            int itemId = 0;
            await fixture.PerformDatabaseOperation(async context =>
            {
                var item = await context.FindAsync<ExpenseCategoryItem>(income!.ID);
                context.Remove(item);
                await context.SaveChangesAsync();
                itemId = item.ID;
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
            ExpenseCategoryItem? income = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(category);
                await context.SaveChangesAsync();
                await context.AddIncome("Test", DateTime.Now, (100, category.ID));
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                income = await context.ExpenseCategoryItems
                    .Include(x => x.Details)
                    .ThenInclude(x => x.ExpenseCategory)
                    .SingleAsync();
            });

            //Assert
            Assert.IsNotNull(income?.Details);
            var item = income!.Details.Single();
            Assert.AreEqual(100, item.Amount);
            Assert.AreEqual(category.ID, item.ExpenseCategory?.ID);
        }
    }
}
