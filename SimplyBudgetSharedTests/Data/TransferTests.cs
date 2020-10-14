using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class TransferTests
    {
        [TestMethod]
        public async Task OnRemove_UpdatesExpenseCategoryAmount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();
            var category1 = new ExpenseCategory { CurrentBalance = 100 };
            var category2 = new ExpenseCategory { CurrentBalance = 200 };

            Transfer? transfer = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category1, category2);
                await context.SaveChangesAsync();
                transfer = await context.AddTransfer("Test", DateTime.Now, 30, category1, category2);
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                transfer = await context.FindAsync<Transfer>(transfer!.ID);
                context.Remove(transfer);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.AreEqual(100, (await context.FindAsync<ExpenseCategory>(category1.ID)).CurrentBalance);
                Assert.AreEqual(200, (await context.FindAsync<ExpenseCategory>(category2.ID)).CurrentBalance);
            });
        }
    }
}
