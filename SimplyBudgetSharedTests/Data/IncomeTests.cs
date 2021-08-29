using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudgetShared.Data;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class IncomeTests
    {
        [TestMethod]
        public async Task OnRemove_RemovesChildren()
        {
            //Arrange
            AutoMocker mocker = new();
            using var factory = mocker.WithDbScope();
            var category = new ExpenseCategory { CurrentBalance = 250 };

            using var setupContext = factory.Create();
            setupContext.Add(category);
            await setupContext.SaveChangesAsync();
            await setupContext.AddIncome("Test", DateTime.Now, (100, category.ID));

            //Act
            using var actContext = factory.Create();
            ExpenseCategoryItem item = await actContext.ExpenseCategoryItems.SingleAsync();
            actContext.Remove(item);
            await actContext.SaveChangesAsync();
            int itemId = item.ID;

            //Assert
            using var assertContext = factory.Create();
            Assert.IsFalse(await assertContext.ExpenseCategoryItems.AnyAsync());
            Assert.IsFalse(await assertContext.ExpenseCategoryItemDetails.AnyAsync());
            Assert.AreEqual(250, (await assertContext.FindAsync<ExpenseCategory>(category.ID)).CurrentBalance);
        }

        [TestMethod]
        public async Task OnRetrieve_GetsRelatedItems()
        {
            //Arrange
            AutoMocker mocker = new();
            using var factory = mocker.WithDbScope();

            var category = new ExpenseCategory { CurrentBalance = 250 };
            using var setupContext = factory.Create();
            setupContext.Add(category);
            await setupContext.SaveChangesAsync();
            await setupContext.AddIncome("Test", DateTime.Now, (100, category.ID));

            //Act
            using var actContext = factory.Create();
            ExpenseCategoryItem income = await actContext.ExpenseCategoryItems
                    .Include(x => x.Details)
                    .ThenInclude(x => x.ExpenseCategory)
                    .SingleAsync();

            //Assert
            Assert.IsNotNull(income?.Details);
            var item = income!.Details.Single();
            Assert.AreEqual(100, item.Amount);
            Assert.AreEqual(category.ID, item.ExpenseCategory?.ID);
        }
    }
}
