using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudgetShared.Data;

namespace SimplyBudgetSharedTests.Data;

[TestClass]
public class TransactionTests
{
    [TestMethod]
    public async Task OnRemove_RemovesChildren()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        ExpenseCategoryItem? transaction = null;
        var category = new ExpenseCategory { CurrentBalance = 250 };
        var setupContext = factory.Create();
        setupContext.Add(category);
        await setupContext.SaveChangesAsync();
        transaction = await setupContext.AddTransaction("Test", DateTime.Now, (100, category.ID));

        //Act
        var actContext = factory.Create();
        var item = await actContext.FindAsync<ExpenseCategoryItem>(transaction!.ID);
        actContext.Remove(item!);
        await actContext.SaveChangesAsync();

        //Assert
        var assertContext = factory.Create();
        Assert.IsFalse(await assertContext.ExpenseCategoryItems.AnyAsync());
        Assert.IsFalse(await assertContext.ExpenseCategoryItemDetails.AnyAsync());
        Assert.AreEqual(250, (await assertContext.FindAsync<ExpenseCategory>(category.ID))?.CurrentBalance);
    }

    [TestMethod]
    public async Task OnRetrieve_GetsRelatedItems()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory { CurrentBalance = 250 };
        var setupContext = factory.Create();
        setupContext.Add(category);
        await setupContext.SaveChangesAsync();
        await setupContext.AddTransaction("Test", DateTime.Now, (100, category.ID));


        //Act
        ExpenseCategoryItem? transaction = null;
        var actContext = factory.Create();
        transaction = await actContext.ExpenseCategoryItems
            .Include(x => x.Details!)
            .ThenInclude(x => x.ExpenseCategory)
            .SingleAsync();

        //Assert
        Assert.IsNotNull(transaction?.Details);
        var item = transaction!.Details.Single();
        Assert.AreEqual(-100, item.Amount);
        Assert.AreEqual(category.ID, item.ExpenseCategory?.ID);
    }
}
