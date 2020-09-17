using IntelliTect.TestTools.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class ExpenseCategoryTests
    {
        [TestMethod]
        public async Task AddTransactionWithoutDate_CreatesTransaction()
        {
            //Arrange
            var fixture = new DatabaseFixture<BudgetContext>();

            const string description = "Test Description";
            const int amount = 100;
            var category = new ExpenseCategory 
            {
                Account = new Account()
            };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.Add(category);
                await context.SaveChangesAsync();
            });

            //Act
            Transaction? transaction = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                transaction = await context.AddTransaction(category, amount, description);
            });

            //Assert
            Assert.IsNotNull(transaction);
            
            await fixture.PerformDatabaseOperation(async context =>
            {
                var foundTransaction = context.Transactions.Single(x => x.ID == transaction!.ID);
                Assert.AreEqual(description, foundTransaction.Description);

                var foundTransactionItem = await context.TransactionItems.SingleOrDefaultAsync(x => x.TransactionID == transaction.ID);
                Assert.AreEqual(amount, foundTransactionItem.Amount);
                Assert.AreEqual(description, foundTransactionItem.Description);
                Assert.AreEqual(category.ID, foundTransactionItem.ExpenseCategoryID);
            });
        }

        [TestMethod]
        public async Task AddTransactionWithDate_CreatesTransaction()
        {
            //Arrange
            var fixture = new DatabaseFixture<BudgetContext>();

            const string description = "Test Description";
            const int amount = 100;
            DateTime transactionDate = DateTime.Now.AddDays(-2);
            var category = new ExpenseCategory
            {
                Account = new Account()
            };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.Add(category);
                await context.SaveChangesAsync();
            });

            //Act
            Transaction? transaction = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                transaction = await context.AddTransaction(category, amount, description, transactionDate);
            });

            //Assert
            Assert.IsNotNull(transaction);

            await fixture.PerformDatabaseOperation(async context =>
            {
                var foundTransaction = context.Transactions.Single(x => x.ID == transaction!.ID);
                Assert.AreEqual(description, foundTransaction.Description);
                Assert.AreEqual(transactionDate.Date, foundTransaction.Date);

                var foundTransactionItem = await context.TransactionItems.SingleOrDefaultAsync(x => x.TransactionID == transaction.ID);
                Assert.AreEqual(amount, foundTransactionItem.Amount);
                Assert.AreEqual(description, foundTransactionItem.Description);
                Assert.AreEqual(category.ID, foundTransactionItem.ExpenseCategoryID);
            });
        }

        [TestMethod]
        public async Task GetTransfersWithoutDateRange_ReturnsTransfersForCategory()
        {
            //Arrange
            var fixture = new DatabaseFixture<BudgetContext>();

            var account = new Account();
            var category1 = new ExpenseCategory { Account = account };
            var category2 = new ExpenseCategory { Account = account };
            var category3 = new ExpenseCategory { Account = account };

            var transfer1 = new Transfer();
            var transfer2 = new Transfer();
            var transfer3 = new Transfer();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.AddRange(category1, category2, category3);
                await context.SaveChangesAsync();

                transfer1.FromExpenseCategoryID = category1.ID;
                transfer1.ToExpenseCategoryID = category2.ID;

                transfer2.FromExpenseCategoryID = category2.ID;
                transfer2.ToExpenseCategoryID = category3.ID;

                transfer3.FromExpenseCategoryID = category3.ID;
                transfer3.ToExpenseCategoryID = category1.ID;

                context.Transfers.AddRange(transfer1, transfer2, transfer3);
                await context.SaveChangesAsync();
            });

            //Act
            IList<Transfer>? transfers = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                transfers = await context.GetTransfers(category2);
            });

            //Assert
            Assert.IsNotNull(transfers);
            CollectionAssert.AreEquivalent(new[] { transfer1.ID, transfer2.ID }, transfers.Select(x => x.ID).ToList());
        }

        [TestMethod]
        public async Task GetTransfersWithDateRange_ReturnsTransfersForCategory()
        {
            //Arrange
            var fixture = new DatabaseFixture<BudgetContext>();

            var account = new Account();
            var category1 = new ExpenseCategory { Account = account };
            var category2 = new ExpenseCategory { Account = account };

            var now = DateTime.Now;
            var transfer1 = new Transfer { Date = now.AddDays(-3).Date };
            var transfer2 = new Transfer { Date = now.AddDays(-2).Date };
            var transfer3 = new Transfer { Date = now.AddDays(-1).Date };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.AddRange(category1, category2);
                await context.SaveChangesAsync();

                transfer1.FromExpenseCategoryID = category1.ID;
                transfer1.ToExpenseCategoryID = category2.ID;

                transfer2.FromExpenseCategoryID = category2.ID;
                transfer2.ToExpenseCategoryID = category1.ID;

                transfer3.FromExpenseCategoryID = category1.ID;
                transfer3.ToExpenseCategoryID = category2.ID;

                context.Transfers.AddRange(transfer1, transfer2, transfer3);
                await context.SaveChangesAsync();
            });

            //Act
            IList<Transfer>? transfers = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                transfers = await context.GetTransfers(category2, now.AddDays(-2).Date, now);
            });

            //Assert
            Assert.IsNotNull(transfers);
            CollectionAssert.AreEquivalent(new[] { transfer2.ID, transfer3.ID }, transfers.Select(x => x.ID).ToList());
        }

        [TestMethod]
        public async Task GetTransactionItemsWithoutDateRange_ReturnsTransactionItems()
        {
            // Arrange
            var fixture = new DatabaseFixture<BudgetContext>();

            var account = new Account();
            var category1 = new ExpenseCategory { Account = account };
            var category2 = new ExpenseCategory { Account = account };

            Transaction? transaction1 = null;
            Transaction? transaction2 = null;
            Transaction? transaction3 = null;

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.AddRange(category1, category2);
                await context.SaveChangesAsync();

                transaction1 = await context.AddTransaction(category1, 100, "Transaction 1");
                transaction2 = await context.AddTransaction(category2, 200, "Transaction 2");
                transaction3 = await context.AddTransaction(category1, 300, "Transaction 3");

            });

            //Act
            IList<TransactionItem>? transactionItems = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                transactionItems = await context.GetTransactionItems(category1);
            });

            //Assert
            Assert.IsNotNull(transactionItems);
            Assert.AreEqual(2, transactionItems!.Count);

            Assert.AreEqual(100, transactionItems[0].Amount);
            Assert.AreEqual("Transaction 1", transactionItems[0].Description);

            Assert.AreEqual(300, transactionItems[1].Amount);
            Assert.AreEqual("Transaction 3", transactionItems[1].Description);
        }

        [TestMethod]
        public async Task GetTransactionItemsWithDateRange_ReturnsTransactionItems()
        {
            // Arrange
            var fixture = new DatabaseFixture<BudgetContext>();

            var account = new Account();
            var category1 = new ExpenseCategory { Account = account };

            DateTime now = DateTime.Now;
            Transaction? transaction1 = null;
            Transaction? transaction2 = null;
            Transaction? transaction3 = null;

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.AddRange(category1);
                await context.SaveChangesAsync();

                transaction1 = await context.AddTransaction(category1, 100, "Transaction 1", now.AddDays(-1).Date);
                transaction2 = await context.AddTransaction(category1, 200, "Transaction 2", now.AddDays(-2).Date);
                transaction3 = await context.AddTransaction(category1, 300, "Transaction 3", now.AddDays(-3).Date);

            });

            //Act
            IList<TransactionItem>? transactionItems = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                transactionItems = await context.GetTransactionItems(category1, now.AddDays(-2).Date, now);
            });

            //Assert
            Assert.IsNotNull(transactionItems);
            Assert.AreEqual(2, transactionItems!.Count);

            Assert.AreEqual(100, transactionItems[0].Amount);
            Assert.AreEqual("Transaction 1", transactionItems[0].Description);

            Assert.AreEqual(200, transactionItems[1].Amount);
            Assert.AreEqual("Transaction 2", transactionItems[1].Description);
        }

        [TestMethod]
        public async Task TestGetIncomeItemsNoDateRange()
        {
            //Arrange
            var category = new ExpenseCategory();
            //var expectedItems = connection.MockQuery<IncomeItem>();

            //Act
            var items = await category.GetIncomeItems();

            //Assert
            //Assert.IsTrue(ReferenceEquals(expectedItems, items));
        }

        [TestMethod]
        public async Task TestGetIncomeItemsWithDateRange()
        {
            //Arrange
            var category = new ExpenseCategory { ID = 1 };
            //var expectedIncome = Mock.Create<Income>();
            var expectedIncomeItem1 = new IncomeItem { ExpenseCategoryID = 1 };
            var expectedIncomeItem2 = new IncomeItem { ExpenseCategoryID = 2 };
            
            //var expectedIncomes = connection.MockQuery<Income>();
            //expectedIncomes.Add(expectedIncome);

            //Mock.Arrange(() => expectedIncome.GetIncomeItems()).Returns(Task.FromResult<IList<IncomeItem>>(new[] { expectedIncomeItem1, expectedIncomeItem2 }));

            //Act
            var items = await category.GetIncomeItems(DateTime.Now, DateTime.Now);

            //Assert
            CollectionAssert.AreEqual(new[] { expectedIncomeItem1 }, items.ToList());
        }

        [TestMethod]
        public async Task TestDeletePostsNotification()
        {
            //Arrange
            var category = new ExpenseCategory { ID = 1 };
            //Mock.SetupStatic<NotificationCenter>();
            //Mock.Arrange(() => connection.DeleteAsync(category)).Returns(Task.FromResult(0));

            //Act
            await category.Delete();

            //Assert
            //Mock.Assert(() => NotificationCenter.PostEvent(Arg.Matches<ExpenseCategoryEvent>(x => ReferenceEquals(x.ExpenseCategory, category) && x.Type == EventType.Deleted)));
            //Mock.Assert(() => connection.DeleteAsync(category), Occurs.Once());
        }

        [TestMethod]
        public async Task TestCreateSetsAccountIDToDefaultAccount()
        {
            //Arrange
            var category = new ExpenseCategory();
            var defaultAccount = new Account { ID = 1 };
            //Mock.SetupStatic<Account>();
            //Mock.Arrange(() => connection.InsertAsync(category)).Returns(Task.FromResult(0));
            //Mock.Arrange(() => Account.GetDefault()).Returns(Task.FromResult(defaultAccount));

            //Act
            await category.Save();

            //Assert
            Assert.AreEqual(defaultAccount.ID, category.AccountID);
            //Mock.Assert(() => connection.InsertAsync(category), Occurs.Once());
        }

        [TestMethod]
        public async Task TestCreateHandlesNullDefaultAccount()
        {
            //Arrange
            var category = new ExpenseCategory();
            //Mock.SetupStatic<Account>();
            //Mock.Arrange(() => connection.InsertAsync(category)).Returns(Task.FromResult(0));
            //Mock.Arrange(() => Account.GetDefault()).Returns(Task.FromResult<Account>(null));

            //Act
            await category.Save();

            //Assert
            Assert.AreEqual(0, category.AccountID);
            //Mock.Assert(() => connection.InsertAsync(category), Occurs.Once());
        }

        [TestMethod]
        public async Task TestCreatePostsNotification()
        {
            //Arrange
            var category = new ExpenseCategory { AccountID = 1 };
            //Mock.SetupStatic<NotificationCenter>();
            //Mock.Arrange(() => connection.InsertAsync(category)).Returns(Task.FromResult(0));

            //Act
            await category.Save();

            //Assert
            //Mock.Assert(() => NotificationCenter.PostEvent(Arg.Matches<ExpenseCategoryEvent>(x => ReferenceEquals(x.ExpenseCategory, category) && x.Type == EventType.Created)));
            //Mock.Assert(() => connection.InsertAsync(category), Occurs.Once());
        }

        [TestMethod]
        public async Task TestUpdatePostsNotification()
        {
            //Arrange
            var category = new ExpenseCategory { ID = 1 };
            //Mock.SetupStatic<NotificationCenter>();
            //Mock.Arrange(() => connection.UpdateAsync(category)).Returns(Task.FromResult(0));

            //Act
            await category.Save();

            //Assert
            //Mock.Assert(() => NotificationCenter.PostEvent(Arg.Matches<ExpenseCategoryEvent>(x => ReferenceEquals(x.ExpenseCategory, category) && x.Type == EventType.Updated)));
            //Mock.Assert(() => connection.UpdateAsync(category), Occurs.Once());
        }

    }
}
