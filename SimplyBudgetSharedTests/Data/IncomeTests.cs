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
            await fixture.PerformDatabaseOperation(async context =>
            {
                income = await context.FindAsync<Income>(income!.ID);
                context.Remove(income);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.IsFalse(await context.Incomes.AnyAsync());
                Assert.IsFalse(await context.IncomeItems.AnyAsync());
                Assert.AreEqual(250, (await context.FindAsync<ExpenseCategory>(category.ID)).CurrentBalance);
            });
        }

        [TestMethod]
        public async Task OnRetrieve_GetsRelatedItems()
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
            await fixture.PerformDatabaseOperation(async context =>
            {
                income = await context.Incomes
                    .Include(x => x.IncomeItems)
                    .ThenInclude(x => x.ExpenseCategory)
                    .SingleAsync();
            });

            //Assert
            Assert.IsNotNull(income?.IncomeItems);
            var item = income!.IncomeItems.Single();
            Assert.AreEqual(100, item.Amount);
            Assert.AreEqual(category.ID, item.ExpenseCategory?.ID);
        }
    }
}
