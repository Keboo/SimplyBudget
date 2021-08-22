using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class AccountTests
    {
        [TestMethod]
        public async Task SetAsDefault_SetsDefaultAccount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();
            var account = new Account();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account);
                await context.SaveChangesAsync();
            });


            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                account = await context.SetAsDefaultAsync(account);
                await context.SaveChangesAsync();
            });


            //Assert
            Assert.IsTrue(account.IsDefault);
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.IsTrue((await context.Accounts.SingleAsync(x => x.ID == account.ID)).IsDefault);
            });
        }

        [TestMethod]
        public async Task SetAsDefault_UpdatesPreviousDefaultAccount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();
            var account = new Account();
            var previousAccount = new Account();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account);
                context.Accounts.Add(previousAccount);
                previousAccount = await context.SetAsDefaultAsync(previousAccount);
                await context.SaveChangesAsync();
            });


            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                account = await context.SetAsDefaultAsync(account);
                await context.SaveChangesAsync();
            });


            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.IsTrue((await context.Accounts.SingleAsync(x => x.ID == account.ID)).IsDefault);
                Assert.IsFalse((await context.Accounts.SingleAsync(x => x.ID == previousAccount.ID)).IsDefault);
            });
        }

        [TestMethod]
        public async Task DeletingDefaultAccount_SelectsFirstAccountAsNewDefault()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();
            var account = new Account();
            var firstAccount = new Account();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account);
                context.Accounts.Add(firstAccount);
                account = await context.SetAsDefaultAsync(account);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Remove(account);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.AreEqual(firstAccount.ID, (await context.GetDefaultAccountAsync())!.ID);
            });
        }

        [TestMethod]
        public async Task GetDefaultAccountAsync_ReturnsDefaultAccount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();
            var account1 = new Account();
            var account2 = new Account();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account1);
                context.Accounts.Add(account2);
                account2 = await context.SetAsDefaultAsync(account2);
                await context.SaveChangesAsync();
            });


            //Act, Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var defaultAccount = await context.GetDefaultAccountAsync();
                Assert.AreEqual(account2.ID, defaultAccount?.ID);
            });
        }

        [TestMethod]
        public async Task CreateAccount_PostsNotification()
        {
            //Arrange
            var watcher = new MessageWatcher<DatabaseEvent<Account>>();
            var fixture = new BudgetDatabaseContext();
            fixture.Messenger.Register(watcher);

            var account1 = new Account();

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account1);
                await context.SaveChangesAsync();
            });

            //Assert
            DatabaseEvent<Account>? message = watcher.Messages.Last();
            Assert.AreEqual(account1.ID, message.Item.ID);
            Assert.AreEqual(EventType.Created, message.Type);
        }

        [TestMethod]
        public async Task UpdateAccount_PostsNotification()
        {
            //Arrange
            var messenger = new WeakReferenceMessenger();
            var watcher = new MessageWatcher<DatabaseEvent<Account>>();
            var fixture = new BudgetDatabaseContext();
            fixture.Messenger.Register(watcher);

            var account1 = new Account();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account1);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                var account = await context.Accounts.FindAsync(account1.ID);
                account.Name += "-Edited";
                await context.SaveChangesAsync();
            });

            //Assert
            DatabaseEvent<Account>? message = watcher.Messages.Last();
            Assert.AreEqual(account1.ID, message.Item.ID);
            Assert.AreEqual(EventType.Updated, message.Type);
        }

        [TestMethod]
        public async Task DeleteAccount_PostsNotification()
        {
            //Arrange
            var messenger = new WeakReferenceMessenger();
            var watcher = new MessageWatcher<DatabaseEvent<Account>>();
            var fixture = new BudgetDatabaseContext();
            fixture.Messenger.Register(watcher);

            var account1 = new Account();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account1);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                var account = await context.Accounts.FindAsync(account1.ID);
                context.Accounts.Remove(account);
                await context.SaveChangesAsync();
            });

            //Assert
            DatabaseEvent<Account>? message = watcher.Messages.Last();
            Assert.AreEqual(account1.ID, message.Item.ID);
            Assert.AreEqual(EventType.Deleted, message.Type);
        }
    }
}
