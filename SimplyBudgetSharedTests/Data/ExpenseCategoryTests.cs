using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;
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
        public async Task TestAddTransactionNoDate()
        {
            //Arrange
            const string DESCRIPTION = "Test Description";
            const int AMOUNT = 100;
            var category = new ExpenseCategory { ID = 1 };
            
            //Mock.Arrange(() => connection.InsertAsync(Arg.Matches<Transaction>(
            //    x => x.Date.Date == DateTime.Today)))
            //    .Returns(Task.FromResult(0)).MustBeCalled();
            //
            //Mock.Arrange(() => connection.InsertAsync(Arg.Matches<TransactionItem>(
            //    x => x.ExpenseCategoryID == category.ID &&
            //         x.Description == DESCRIPTION &&
            //         x.Amount == AMOUNT)))
            //         .Returns(Task.FromResult(0)).MustBeCalled();
            //
            //Mock.Arrange(() => connection.GetAsync<ExpenseCategory>(Arg.AnyObject)).Returns(Task.FromResult<ExpenseCategory>(null));

            //Act
            var transaction = await category.AddTransaction(AMOUNT, DESCRIPTION);

            //Assert
            Assert.IsNotNull(transaction);
            //Mock.Assert(() => connection.InsertAsync(Arg.AnyObject), Occurs.Exactly(2));
        }

        [TestMethod]
        public async Task TestAddTransactionWithDate()
        {
            //Arrange
            const string DESCRIPTION = "Test Description";
            const int AMOUNT = 100;
            DateTime yesterday = DateTime.Today.Subtract(TimeSpan.FromDays(1));
            var category = new ExpenseCategory { ID = 1 };
            
            //Mock.Arrange(() => connection.InsertAsync(Arg.Matches<Transaction>(
            //    x => x.Date == yesterday)))
            //    .Returns(Task.FromResult(0)).MustBeCalled();
            //
            //Mock.Arrange(() => connection.InsertAsync(Arg.Matches<TransactionItem>(
            //    x => x.ExpenseCategoryID == category.ID &&
            //         x.Description == DESCRIPTION &&
            //         x.Amount == AMOUNT)))
            //         .Returns(Task.FromResult(0)).MustBeCalled();
            //
            //Mock.Arrange(() => connection.GetAsync<ExpenseCategory>(Arg.AnyObject)).Returns(Task.FromResult<ExpenseCategory>(null));

            //Act
            var transaction = await category.AddTransaction(AMOUNT, DESCRIPTION, yesterday);

            //Assert
            Assert.IsNotNull(transaction);
            //Mock.Assert(() => connection.InsertAsync(Arg.AnyObject), Occurs.Exactly(2));
        }

        [TestMethod]
        public async Task TestGetTransfersNoDateRange()
        {
            //Arrange
            var category = new ExpenseCategory();
            //var expectedTransfers = connection.MockQuery<Transfer>();

            //Act
            var transfers = await category.GetTransfers();

            //Assert
            //Assert.IsTrue(ReferenceEquals(expectedTransfers, transfers));
        }

        [TestMethod]
        public async Task TestGetTransfersWithDateRange()
        {
            //Arrange
            var category = new ExpenseCategory();
            
            //var expectedTransfers = connection.MockQuery<Transfer>();

            //Act
            var transfers = await category.GetTransfers(DateTime.Now, DateTime.Now);

            //Assert
            //Assert.IsTrue(ReferenceEquals(expectedTransfers, transfers));
        }

        [TestMethod]
        public async Task TestGetTransactionItemsNoDateRange()
        {
            //Arrange
            var category = new ExpenseCategory();
            
            //var expectedItems = connection.MockQuery<TransactionItem>();

            //Act
            var items = await category.GetTransactionItems();

            //Assert
            //Assert.IsTrue(ReferenceEquals(expectedItems, items));
        }

        [TestMethod]
        public async Task TestGetTransactionItemsWithDateRange()
        {
            //Arrange
            var category = new ExpenseCategory { ID = 1 };
            //var expectedTransaction = Mock.Create<Transaction>();
            var expectedTransactionItem1 = new TransactionItem { ExpenseCategoryID = 1 };
            var expectedTransactionItem2 = new TransactionItem { ExpenseCategoryID = 2 };
            
            //var expectedTransactions = connection.MockQuery<Transaction>();
            //expectedTransactions.Add(expectedTransaction);

            //Mock.Arrange(() => expectedTransaction.GetTransactionItems()).Returns(Task.FromResult<IList<TransactionItem>>(new[] { expectedTransactionItem1, expectedTransactionItem2 }));

            //Act
            var items = await category.GetTransactionItems(DateTime.Now, DateTime.Now);

            //Assert
            CollectionAssert.AreEqual(new[] { expectedTransactionItem1 }, items.ToList());
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
            var defaultAccount = new Account { ID = 1, IsDefault = true };
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
