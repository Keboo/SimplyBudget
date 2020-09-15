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

        [TestMethod, Ignore]
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

        [TestMethod, Ignore]
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
        public async Task DeletingDefaultAccount_SelectsFirstAccountAsNewDefault()
        {
            //Arrange
            var fixture = new DatabaseFixture<BudgetContext>();
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
                Assert.IsTrue((await context.Accounts.SingleAsync(x => x.ID == firstAccount.ID)).IsDefault);
            });
        }

        [TestMethod, Ignore]
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
        public async Task GetDefaultAccountAsync_ReturnsDefaultAccount()
        {
            //Arrange
            var fixture = new DatabaseFixture<BudgetContext>();
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
                Assert.AreEqual(account2.ID, defaultAccount.ID);
            });
        }
    }
}
