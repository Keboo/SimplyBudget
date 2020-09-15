using IntelliTect.TestTools.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
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
            var fixture = new DatabaseFixture<BudgetContext>();
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
            var fixture = new DatabaseFixture<BudgetContext>();
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
        public async Task CreateAsDefaultClearsPreviousDefault()
        {
            //Arrange
            var fixture = new DatabaseFixture<BudgetContext>();
            var account = new Account();
            var previousDefault = new Account ();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(previousDefault);
                await context.SaveChangesAsync();
            });


            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account);
                await context.SaveChangesAsync();
            });


            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.IsFalse((await context.Accounts.SingleAsync(x => x.ID == previousDefault.ID)).IsDefault);
                Assert.IsTrue((await context.Accounts.SingleAsync(x => x.ID == account.ID)).IsDefault);
            });
        }

        [TestMethod]
        public async Task TestCreatePostsNotification()
        {
            //Arrange
            var account = new Account();
            //Mock.SetupStatic<NotificationCenter>();
            //Mock.Arrange(() => connection.InsertAsync(account)).Returns(Task.FromResult(0));

            //Act
            await account.Save();

            //Assert
            //Mock.Assert(() => NotificationCenter.PostEvent(Arg.Matches<AccountEvent>(x => ReferenceEquals(x.Account, account) && x.Type == EventType.Created)));
        }

        [TestMethod]
        public async Task TestUpdateAsDefaultClearsPreviousDefault()
        {
            //Arrange
            var account = new Account { ID = 1 };
            var previousDefault = new Account { ID = 2 };

            //Mock.Arrange(() => connection.UpdateAsync(account)).Returns(Task.FromResult(0));

            //Mock.SetupStatic<Account>(Behavior.Strict);
            //Mock.Arrange(() => Account.GetDefault()).Returns(Task.FromResult(previousDefault));
            //Mock.Arrange(() => connection.UpdateAsync(previousDefault)).Returns(Task.FromResult(0));

            //Act
            await account.Save();

            //Assert
            Assert.IsFalse(previousDefault.IsDefault);
            Assert.IsTrue(account.IsDefault);
            //Mock.Assert(() => connection.UpdateAsync(account), Occurs.Once());
            //Mock.Assert(() => connection.UpdateAsync(previousDefault), Occurs.Once());
        }

        [TestMethod]
        public async Task TestUpdateAsDefaultHandlesNoPreviousDefault()
        {
            //Arrange
            var account = new Account { ID = 1 };

            //Mock.Arrange(() => connection.UpdateAsync(account)).Returns(Task.FromResult(0));

            //Mock.SetupStatic<Account>(Behavior.Strict);
            //Mock.Arrange(() => Account.GetDefault()).Returns(Task.FromResult((Account)null));

            //Act
            await account.Save();

            //Assert
            Assert.IsTrue(account.IsDefault);
            //Mock.Assert(() => connection.UpdateAsync(account), Occurs.Once());
        }

        [TestMethod]
        public async Task TestUpdateAsDefaultHandlesItselfBeingTheDefault()
        {
            //Arrange
            var account = new Account { ID = 1 };
            var duplicateAccount = new Account { ID = 1 };

            //Mock.Arrange(() => connection.UpdateAsync(account)).Returns(Task.FromResult(0));

            //Mock.SetupStatic<Account>(Behavior.Strict);
            //Mock.Arrange(() => Account.GetDefault()).Returns(Task.FromResult(duplicateAccount));

            //Act
            await account.Save();

            //Assert
            Assert.IsTrue(account.IsDefault);
            //Mock.Assert(() => connection.UpdateAsync(account), Occurs.Once());
        }

        [TestMethod]
        public async Task TestUpdatePostsNotification()
        {
            //Arrange
            var account = new Account { ID = 1 };
            //Mock.SetupStatic<NotificationCenter>();
            //Mock.Arrange(() => connection.UpdateAsync(account)).Returns(Task.FromResult(0));

            //Act
            await account.Save();

            //Assert
            //Mock.Assert(() => NotificationCenter.PostEvent(Arg.Matches<AccountEvent>(x => ReferenceEquals(x.Account, account) && x.Type == EventType.Updated)));
        }

        [TestMethod]
        public async Task TestDeletingDefaultSelectsFirstAccountAsNewDefault()
        {
            //Arrange
            var account = new Account { ID = 1 };
            var firstAccount = new Account { ID = 2 };
            //Mock.Arrange(() => connection.DeleteAsync(account)).Returns(Task.FromResult(0));

            //var tableQuery = Mock.Create<AsyncTableQuery<Account>>();
            //Mock.Arrange(() => connection.Table<Account>()).Returns(tableQuery);
            //Mock.Arrange(() => tableQuery.FirstOrDefaultAsync()).Returns(Task.FromResult(firstAccount));
            //Mock.Arrange(() => connection.UpdateAsync(firstAccount)).Returns(Task.FromResult(0));
            //Mock.SetupStatic<Account>();
            //Mock.Arrange(() => Account.GetDefault()).Returns(Task.FromResult((Account) null));

            //Act
            await account.Delete();

            //Assert
            Assert.IsTrue(firstAccount.IsDefault);
            //Mock.Assert(() => connection.DeleteAsync(account), Occurs.Once());
            //Mock.Assert(() => connection.UpdateAsync(firstAccount), Occurs.Once());
        }

        [TestMethod]
        public async Task TestDeletingDefaultWhenItIsTheLastAccount()
        {
            //Arrange
            var account = new Account { ID = 1 };
            //Mock.Arrange(() => connection.DeleteAsync(account)).Returns(Task.FromResult(0));

            //var tableQuery = Mock.Create<AsyncTableQuery<Account>>();
            //Mock.Arrange(() => connection.Table<Account>()).Returns(tableQuery);
            //Mock.Arrange(() => tableQuery.FirstOrDefaultAsync()).Returns(Task.FromResult((Account)null));

            //Act
            await account.Delete();

            //Assert
            //Mock.Assert(() => connection.DeleteAsync(account), Occurs.Once());
        }

        [TestMethod]
        public async Task TestDeletePostsNotification()
        {
            //Arrange
            var account = new Account { ID = 1 };
            //Mock.SetupStatic<NotificationCenter>();
            //Mock.Arrange(() => connection.DeleteAsync(account)).Returns(Task.FromResult(0));

            //Act
            await account.Delete();

            //Assert
            //Mock.Assert(() => NotificationCenter.PostEvent(Arg.Matches<AccountEvent>(x => ReferenceEquals(x.Account, account) && x.Type == EventType.Deleted)));
        }

        [TestMethod]
        public async Task TestGetDefault()
        {
            //Arrange
            var account = new Account();
            //var tableQuery = Mock.Create<AsyncTableQuery<Account>>();
            //Mock.Arrange(() => connection.Table<Account>()).Returns(tableQuery);
            //Mock.Arrange(() => tableQuery.Where(Arg.IsAny<Expression<Func<Account, bool>>>())).Returns(tableQuery);
            //Mock.Arrange(() => tableQuery.FirstOrDefaultAsync()).Returns(Task.FromResult(account));

            //Act
            var defaultAccount = await Account.GetDefault();

            //Assert
            Assert.IsTrue(ReferenceEquals(account, defaultAccount));
        }
    }
}
