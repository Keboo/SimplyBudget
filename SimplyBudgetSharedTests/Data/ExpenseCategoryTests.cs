using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;

namespace SimplyBudgetSharedTests.Data;

[TestClass]
public class ExpenseCategoryTests
{
    [TestMethod]
    public async Task AddTransactionWithoutDate_CreatesTransaction()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        const string description = "Test Description";
        const int amount = 100;
        var category = new ExpenseCategory
        {
            Account = new Account()
        };

        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.Add(category);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        ExpenseCategoryItem transaction = await actContext.AddTransaction(category.ID, amount, description);

        //Assert
        using var assertContext = factory.Create();
        var foundTransaction = assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .Single(x => x.ID == transaction!.ID);
        Assert.AreEqual(description, foundTransaction.Description);

        Assert.AreEqual(1, foundTransaction.Details?.Count);
        Assert.AreEqual(-amount, foundTransaction.Details?[0].Amount);
        Assert.AreEqual(category.ID, foundTransaction.Details?[0].ExpenseCategoryId);
    }

    [TestMethod]
    public async Task AddTransactionWithDate_CreatesTransaction()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        const string description = "Test Description";
        const int amount = 100;
        DateTime transactionDate = DateTime.Now.AddDays(-2);
        var category = new ExpenseCategory
        {
            Account = new Account()
        };

        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.Add(category);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        ExpenseCategoryItem transaction = await actContext.AddTransaction(category.ID, amount, description, transactionDate);

        //Assert
        using var assertContext = factory.Create();
        var foundTransaction = await assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync(x => x.ID == transaction!.ID);
        Assert.AreEqual(description, foundTransaction.Description);
        Assert.AreEqual(transactionDate.Date, foundTransaction.Date);

        var foundTransactionItem = foundTransaction.Details!.Single();
        Assert.AreEqual(-amount, foundTransactionItem.Amount);
        Assert.AreEqual(category.ID, foundTransactionItem.ExpenseCategoryId);
    }

    [TestMethod]
    public async Task GetTransfersWithoutDateRange_ReturnsTransfersForCategory()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account = new Account();
        var category1 = new ExpenseCategory { Account = account };
        var category2 = new ExpenseCategory { Account = account };
        var category3 = new ExpenseCategory { Account = account };

        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.AddRange(category1, category2, category3);
        await setupContext.SaveChangesAsync();

        ExpenseCategoryItem transfer1 = await setupContext.AddTransfer("", DateTime.Now, 10, category1, category2);
        ExpenseCategoryItem transfer2 = await setupContext.AddTransfer("", DateTime.Now, 10, category2, category3);
        ExpenseCategoryItem transfer3 = await setupContext.AddTransfer("", DateTime.Now, 10, category3, category1);

        //Act
        using var actContext = factory.Create();
        IList<ExpenseCategoryItem> transfers = await actContext.GetTransfers(category2);

        //Assert
        CollectionAssert.AreEquivalent(new[] { transfer1.ID, transfer2.ID }, transfers.Select(x => x.ID).ToList());
    }

    [TestMethod]
    public async Task GetTransfersWithDateRange_ReturnsTransfersForCategory()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account = new Account();
        var category1 = new ExpenseCategory { Account = account };
        var category2 = new ExpenseCategory { Account = account };

        var now = DateTime.Now;
        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.AddRange(category1, category2);
        await setupContext.SaveChangesAsync();

        ExpenseCategoryItem transfer1 = await setupContext.AddTransfer("", now.AddDays(-3).Date, 10, category1, category2);
        ExpenseCategoryItem transfer2 = await setupContext.AddTransfer("", now.AddDays(-2).Date, 10, category2, category1);
        ExpenseCategoryItem transfer3 = await setupContext.AddTransfer("", now.AddDays(-1).Date, 10, category1, category2);

        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        IList<ExpenseCategoryItem> transfers = await actContext.GetTransfers(category2, now.AddDays(-2).Date, now);

        //Assert
        CollectionAssert.AreEquivalent(new[] { transfer2.ID, transfer3.ID }, transfers.Select(x => x.ID).ToList());
    }

    [TestMethod]
    public async Task GetTransactionItemsWithoutDateRange_ReturnsTransactionItems()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account = new Account();
        var category1 = new ExpenseCategory { Account = account };
        var category2 = new ExpenseCategory { Account = account };

        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.AddRange(category1, category2);
        await setupContext.SaveChangesAsync();

        ExpenseCategoryItem transaction1 = await setupContext.AddTransaction(category1.ID, 100, "Transaction 1");
        ExpenseCategoryItem transaction2 = await setupContext.AddTransaction(category2.ID, 200, "Transaction 2");
        ExpenseCategoryItem transaction3 = await setupContext.AddTransaction(category1.ID, 300, "Transaction 3");

        //Act
        using var actContext = factory.Create();
        IList<ExpenseCategoryItemDetail> transactionItems = await actContext.GetCategoryItemDetails(category1);

        //Assert
        Assert.AreEqual(2, transactionItems.Count);

        Assert.AreEqual(-100, transactionItems[0].Amount);
        Assert.AreEqual("Transaction 1", transactionItems[0].ExpenseCategoryItem?.Description);

        Assert.AreEqual(-300, transactionItems[1].Amount);
        Assert.AreEqual("Transaction 3", transactionItems[1].ExpenseCategoryItem?.Description);
    }

    [TestMethod]
    public async Task GetTransactionItemsWithDateRange_ReturnsTransactionItems()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account = new Account();
        var category1 = new ExpenseCategory { Account = account };

        DateTime now = DateTime.Now;
        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.AddRange(category1);
        await setupContext.SaveChangesAsync();

        ExpenseCategoryItem transaction1 = await setupContext.AddTransaction(category1.ID, 100, "Transaction 1", now.AddDays(-1).Date);
        ExpenseCategoryItem transaction2 = await setupContext.AddTransaction(category1.ID, 200, "Transaction 2", now.AddDays(-2).Date);
        ExpenseCategoryItem transaction3 = await setupContext.AddTransaction(category1.ID, 300, "Transaction 3", now.AddDays(-3).Date);

        //Act
        using var actContext = factory.Create();
        IList<ExpenseCategoryItemDetail> transactionItems = await actContext.GetCategoryItemDetails(category1, now.AddDays(-2).Date, now);

        //Assert
        Assert.AreEqual(2, transactionItems.Count);

        Assert.AreEqual(-100, transactionItems[0].Amount);
        Assert.AreEqual("Transaction 1", transactionItems[0].ExpenseCategoryItem?.Description);

        Assert.AreEqual(-200, transactionItems[1].Amount);
        Assert.AreEqual("Transaction 2", transactionItems[1].ExpenseCategoryItem?.Description);
    }

    [TestMethod]
    public async Task GetIncomeItems_WithoutDateRange_ReturnsIncomeItems()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account = new Account();
        var category1 = new ExpenseCategory { Account = account };

        DateTime now = DateTime.Now;

        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.AddRange(category1);
        await setupContext.SaveChangesAsync();

        await setupContext.AddIncome("Income description", now.AddDays(-1),
            (100, category1.ID),
            (200, category1.ID));

        //Act
        using var actContext = factory.Create();
        IList<ExpenseCategoryItemDetail>? incomeItems = await actContext.GetCategoryItemDetails(category1);

        //Assert
        Assert.IsNotNull(incomeItems);
        Assert.AreEqual(2, incomeItems!.Count);

        Assert.AreEqual(100, incomeItems[0].Amount);
        Assert.AreEqual("Income description", incomeItems[0].ExpenseCategoryItem?.Description);

        Assert.AreEqual(200, incomeItems[1].Amount);
        Assert.AreEqual("Income description", incomeItems[1].ExpenseCategoryItem?.Description);
    }

    [TestMethod]
    public async Task GetIncomeItems_WithDateRange_ReturnsIncomeItems()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account = new Account();
        var category1 = new ExpenseCategory { Account = account };

        DateTime now = DateTime.Today;
        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.AddRange(category1);
        await setupContext.SaveChangesAsync();

        await setupContext.AddIncome("Income 1 description", now.AddDays(-1), (100, category1.ID), (200, category1.ID));
        await setupContext.AddIncome("Income 2 description", now.AddDays(-2), (200, category1.ID), (300, category1.ID));

        //Act
        using var actContext = factory.Create();
        IList<ExpenseCategoryItemDetail> incomeItems = await actContext.GetCategoryItemDetails(category1, now.AddDays(-3), now.AddDays(-2));

        //Assert
        Assert.IsNotNull(incomeItems);
        Assert.AreEqual(2, incomeItems!.Count);

        Assert.AreEqual("Income 2 description", incomeItems[0].ExpenseCategoryItem?.Description);
        Assert.AreEqual("Income 2 description", incomeItems[1].ExpenseCategoryItem?.Description);

        CollectionAssert.AreEquivalent(new[] { 200, 300 }, incomeItems.Select(x => x.Amount).ToArray());
    }

    [TestMethod]
    public async Task CreateExpenseCategory_WithoutAccount_SetsAccountToDefaultAccount()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account = new Account();
        var category = new ExpenseCategory();

        using var setupContext = factory.Create();
        setupContext.Accounts.Add(account);
        await setupContext.SaveChangesAsync();
        await setupContext.SetAsDefaultAsync(account);

        //Act
        using var actContext = factory.Create();
        actContext.ExpenseCategories.Add(category);
        await actContext.SaveChangesAsync();

        //Assert
        using var assertContext = factory.Create();
        var expenseCategory = await assertContext.ExpenseCategories.FindAsync(category.ID);
        Assert.AreEqual(account.ID, expenseCategory.AccountID);
    }

    [TestMethod]
    public async Task CreateExpenseCategory_AllowsNullDefaultAccount()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory();

        //Act
        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.Add(category);
        await setupContext.SaveChangesAsync();

        //Assert
        using var assertContext = factory.Create();
        var expenseCategory = await assertContext.ExpenseCategories.FindAsync(category.ID);
        Assert.AreEqual(null, expenseCategory!.AccountID);
    }

    [TestMethod]
    public async Task DeleteExpenseCategory_PostsNotification()
    {
        //Arrange
        var messenger = new WeakReferenceMessenger();
        var watcher = new MessageWatcher<DatabaseEvent<ExpenseCategory>>();
        messenger.Register(watcher);
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope(messenger);

        var expenseCategory = new ExpenseCategory
        {
            Account = new Account()
        };

        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.Add(expenseCategory);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        var category = await actContext.ExpenseCategories.FindAsync(expenseCategory.ID);
        actContext.ExpenseCategories.Remove(category);
        await actContext.SaveChangesAsync();

        //Assert
        DatabaseEvent<ExpenseCategory> message = watcher.Messages.Last();
        Assert.AreEqual(expenseCategory.ID, message.Item.ID);
        Assert.AreEqual(EventType.Deleted, message.Type);
    }

    [TestMethod]
    public async Task CreateExpenseCategory_PostsNotification()
    {
        //Arrange
        var messenger = new WeakReferenceMessenger();
        var watcher = new MessageWatcher<DatabaseEvent<ExpenseCategory>>();
        messenger.Register(watcher);
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope(messenger);

        var expenseCategory = new ExpenseCategory
        {
            Account = new Account()
        };

        //Act
        using var actContext = factory.Create();
        actContext.ExpenseCategories.Add(expenseCategory);
        await actContext.SaveChangesAsync();

        //Assert
        DatabaseEvent<ExpenseCategory>? message = watcher.Messages.Last();
        Assert.AreEqual(expenseCategory.ID, message.Item.ID);
        Assert.AreEqual(EventType.Created, message.Type);
    }

    [TestMethod]
    public async Task UpdateExpenseCategory_PostsNotification()
    {
        //Arrange
        var messenger = new WeakReferenceMessenger();
        var watcher = new MessageWatcher<DatabaseEvent<ExpenseCategory>>();
        messenger.Register(watcher);
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope(messenger);

        var expenseCategory = new ExpenseCategory
        {
            Account = new Account()
        };

        using var setupContext = factory.Create();
        setupContext.ExpenseCategories.Add(expenseCategory);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        var category = await actContext.ExpenseCategories.FindAsync(expenseCategory.ID);
        category.CategoryName += "-Edited";
        await actContext.SaveChangesAsync();

        //Assert
        DatabaseEvent<ExpenseCategory>? message = watcher.Messages.Last();
        Assert.AreEqual(expenseCategory.ID, message.Item.ID);
        Assert.AreEqual(EventType.Updated, message.Type);
    }

}
