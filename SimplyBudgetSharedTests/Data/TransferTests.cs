using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudgetShared.Data;
using System;
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
            AutoMocker mocker = new();
            using var factory = mocker.WithDbScope();
            var category1 = new ExpenseCategory { CurrentBalance = 100 };
            var category2 = new ExpenseCategory { CurrentBalance = 200 };

            ExpenseCategoryItem? transfer = null;
            using var setupContext = factory.Create();
            setupContext.AddRange(category1, category2);
            await setupContext.SaveChangesAsync();
            transfer = await setupContext.AddTransfer("Test", DateTime.Now, 30, category1, category2);

            //Act
            using var actContext = factory.Create();
            var item = await actContext.FindAsync<ExpenseCategoryItem>(transfer!.ID);
            actContext.Remove(item);
            await actContext.SaveChangesAsync();

            //Assert
            using var assertContext = factory.Create();
            Assert.AreEqual(100, (await assertContext.FindAsync<ExpenseCategory>(category1.ID)).CurrentBalance);
            Assert.AreEqual(200, (await assertContext.FindAsync<ExpenseCategory>(category2.ID)).CurrentBalance);
        }

        [TestMethod]
        public async Task OnRetrieve_GetsRelatedItems()
        {
            //Arrange
            AutoMocker mocker = new();
            using var factory = mocker.WithDbScope();

            var category1 = new ExpenseCategory { CurrentBalance = 100 };
            var category2 = new ExpenseCategory { CurrentBalance = 200 };

            ExpenseCategoryItem? transfer = null;
            using var setupContext = factory.Create();
            setupContext.AddRange(category1, category2);
            await setupContext.SaveChangesAsync();
            await setupContext.AddTransfer("Test", DateTime.Now, 30, category1, category2);


            //Act
            using var actContext = factory.Create();
            transfer = await actContext.ExpenseCategoryItems
                .Include(x => x.Details)
                .ThenInclude(x => x.ExpenseCategory)
                .SingleAsync();

            //Assert
            Assert.AreEqual(2, transfer?.Details?.Count);
            Assert.AreEqual(category1.ID, transfer?.Details?[0]?.ID);
            Assert.AreEqual(category2.ID, transfer?.Details?[1]?.ID);
        }
    }
}
