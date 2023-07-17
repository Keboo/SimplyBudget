using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;

namespace SimplyBudgetSharedTests.Data;

[TestClass]
public class AccountTests
{
    [TestMethod]
    public async Task SetAsDefault_SetsDefaultAccount()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();
        var account = new Account();

        using var setupContext = factory.Create();
        setupContext.Accounts.Add(account);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        account = await actContext.SetAsDefaultAsync(account);
        await actContext.SaveChangesAsync();

        //Assert
        Assert.IsTrue(account.IsDefault);
        using var assertContext = factory.Create();
        Assert.IsTrue((await assertContext.Accounts.SingleAsync(x => x.ID == account.ID)).IsDefault);
    }

    [TestMethod]
    public async Task SetAsDefault_UpdatesPreviousDefaultAccount()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();
        var account = new Account();
        var previousAccount = new Account();

        using var setupContext = factory.Create();
        setupContext.Accounts.Add(account);
        setupContext.Accounts.Add(previousAccount);
        previousAccount = await setupContext.SetAsDefaultAsync(previousAccount);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        account = await actContext.SetAsDefaultAsync(account);
        await actContext.SaveChangesAsync();


        //Assert
        using var assertContext = factory.Create();
        Assert.IsTrue((await assertContext.Accounts.SingleAsync(x => x.ID == account.ID)).IsDefault);
        Assert.IsFalse((await assertContext.Accounts.SingleAsync(x => x.ID == previousAccount.ID)).IsDefault);
    }

    [TestMethod]
    public async Task DeletingDefaultAccount_SelectsFirstAccountAsNewDefault()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();
        var account = new Account();
        var firstAccount = new Account();

        using var setupContext = factory.Create();
        setupContext.Accounts.Add(account);
        setupContext.Accounts.Add(firstAccount);
        account = await setupContext.SetAsDefaultAsync(account);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        actContext.Accounts.Remove(account);
        await actContext.SaveChangesAsync();

        //Assert
        using var assertContext = factory.Create();
        Assert.AreEqual(firstAccount.ID, (await assertContext.GetDefaultAccountAsync())!.ID);
    }

    [TestMethod]
    public async Task GetDefaultAccountAsync_ReturnsDefaultAccount()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account1 = new Account();
        var account2 = new Account();

        using var setupContext = factory.Create();
        setupContext.Accounts.Add(account1);
        setupContext.Accounts.Add(account2);
        account2 = await setupContext.SetAsDefaultAsync(account2);
        await setupContext.SaveChangesAsync();

        //Act, Assert
        using var context = factory.Create();
        var defaultAccount = await context.GetDefaultAccountAsync();
        Assert.AreEqual(account2.ID, defaultAccount?.ID);
    }

    [TestMethod]
    public async Task CreateAccount_PostsNotification()
    {
        //Arrange
        var mocker = new AutoMocker().WithMessenger();
        var watcher = new MessageWatcher<DatabaseEvent<Account>>();
        mocker.Get<IMessenger>().Register(watcher);
        using var factory = mocker.WithDbScope();

        var account1 = new Account();

        //Act
        using var context = factory.Create();
        context.Accounts.Add(account1);
        await context.SaveChangesAsync();

        //Assert
        DatabaseEvent<Account>? message = watcher.Messages.Last();
        Assert.AreEqual(account1.ID, message.Item.ID);
        Assert.AreEqual(EventType.Created, message.Type);
    }

    [TestMethod]
    public async Task UpdateAccount_PostsNotification()
    {
        //Arrange
        var mocker = new AutoMocker().WithMessenger();
        var watcher = new MessageWatcher<DatabaseEvent<Account>>();
        mocker.Get<IMessenger>().Register(watcher);
        using var factory = mocker.WithDbScope();

        var account1 = new Account();

        using var setupContext = factory.Create();
        setupContext.Accounts.Add(account1);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        var account = await actContext.Accounts.FindAsync(account1.ID);
        account!.Name += "-Edited";
        await actContext.SaveChangesAsync();

        //Assert
        DatabaseEvent<Account>? message = watcher.Messages.Last();
        Assert.AreEqual(account1.ID, message.Item.ID);
        Assert.AreEqual(EventType.Updated, message.Type);
    }

    [TestMethod]
    public async Task DeleteAccount_PostsNotification()
    {
        //Arrange
        var mocker = new AutoMocker().WithMessenger();
        var watcher = new MessageWatcher<DatabaseEvent<Account>>();
        mocker.Get<IMessenger>().Register(watcher);
        using var factory = mocker.WithDbScope();

        var account1 = new Account();

        using var setupContext = factory.Create();
        setupContext.Accounts.Add(account1);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        var account = await actContext.Accounts.FindAsync(account1.ID);
        actContext.Accounts.Remove(account!);
        await actContext.SaveChangesAsync();

        //Assert
        DatabaseEvent<Account>? message = watcher.Messages.Last();
        Assert.AreEqual(account1.ID, message.Item.ID);
        Assert.AreEqual(EventType.Deleted, message.Type);
    }
}
