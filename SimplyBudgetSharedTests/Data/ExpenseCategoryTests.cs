using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
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
            var fixture = new BudgetDatabaseContext();

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
                transaction = await context.AddTransaction(category.ID, amount, description);
            });

            //Assert
            Assert.IsNotNull(transaction);
            
            await fixture.PerformDatabaseOperation(async context =>
            {
                var foundTransaction = context.Transactions.Single(x => x.ID == transaction!.ID);
                Assert.AreEqual(description, foundTransaction.Description);

                var foundTransactionItem = await context.TransactionItems.SingleOrDefaultAsync(x => x.TransactionID == transaction!.ID);
                Assert.AreEqual(amount, foundTransactionItem.Amount);
                Assert.AreEqual(description, foundTransactionItem.Description);
                Assert.AreEqual(category.ID, foundTransactionItem.ExpenseCategoryID);
            });
        }

        [TestMethod]
        public async Task AddTransactionWithDate_CreatesTransaction()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

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
                transaction = await context.AddTransaction(category.ID, amount, description, transactionDate);
            });

            //Assert
            Assert.IsNotNull(transaction);

            await fixture.PerformDatabaseOperation(async context =>
            {
                var foundTransaction = context.Transactions.Single(x => x.ID == transaction!.ID);
                Assert.AreEqual(description, foundTransaction.Description);
                Assert.AreEqual(transactionDate.Date, foundTransaction.Date);

                var foundTransactionItem = await context.TransactionItems.SingleOrDefaultAsync(x => x.TransactionID == transaction!.ID);
                Assert.AreEqual(amount, foundTransactionItem.Amount);
                Assert.AreEqual(description, foundTransactionItem.Description);
                Assert.AreEqual(category.ID, foundTransactionItem.ExpenseCategoryID);
            });
        }

        [TestMethod]
        public async Task GetTransfersWithoutDateRange_ReturnsTransfersForCategory()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

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
            var fixture = new BudgetDatabaseContext();

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
            var fixture = new BudgetDatabaseContext();

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

                transaction1 = await context.AddTransaction(category1.ID, 100, "Transaction 1");
                transaction2 = await context.AddTransaction(category2.ID, 200, "Transaction 2");
                transaction3 = await context.AddTransaction(category1.ID, 300, "Transaction 3");

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
            var fixture = new BudgetDatabaseContext();

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

                transaction1 = await context.AddTransaction(category1.ID, 100, "Transaction 1", now.AddDays(-1).Date);
                transaction2 = await context.AddTransaction(category1.ID, 200, "Transaction 2", now.AddDays(-2).Date);
                transaction3 = await context.AddTransaction(category1.ID, 300, "Transaction 3", now.AddDays(-3).Date);

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
        public async Task GetIncomeItems_WithoutDateRange_ReturnsIncomeItems()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var account = new Account();
            var category1 = new ExpenseCategory { Account = account };

            DateTime now = DateTime.Now;

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.AddRange(category1);
                var income = new Income
                {
                    Date = now.AddDays(-1),
                    TotalAmount = 300,
                    Description = "Income description"
                };
                context.Incomes.Add(income);
                await context.SaveChangesAsync();

                await context.AddIncomeItem(category1, income, 100, "Income item 1");
                await context.AddIncomeItem(category1, income, 200);
            });

            //Act
            IList<IncomeItem>? incomeItems = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                incomeItems = await context.GetIncomeItems(category1);
            });

            //Assert
            Assert.IsNotNull(incomeItems);
            Assert.AreEqual(2, incomeItems!.Count);

            Assert.AreEqual(100, incomeItems[0].Amount);
            Assert.AreEqual("Income item 1", incomeItems[0].Description);

            Assert.AreEqual(200, incomeItems[1].Amount);
            Assert.AreEqual("Income description", incomeItems[1].Description);
        }

        [TestMethod]
        public async Task GetIncomeItems_WithDateRange_ReturnsIncomeItems()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var account = new Account();
            var category1 = new ExpenseCategory { Account = account };

            DateTime now = DateTime.Today;

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.AddRange(category1);
                var income1 = new Income
                {
                    Date = now.AddDays(-1),
                    TotalAmount = 300,
                    Description = "Income 1 description"
                };
                context.Incomes.Add(income1);
                var income2 = new Income
                {
                    Date = now.AddDays(-2),
                    TotalAmount = 500,
                    Description = "Income 2 description"
                };
                context.Incomes.Add(income2);
                await context.SaveChangesAsync();

                await context.AddIncomeItem(category1, income1, 100, "Income item 1");
                await context.AddIncomeItem(category1, income1, 200);

                await context.AddIncomeItem(category1, income2, 200, "Income item 3");
                await context.AddIncomeItem(category1, income2, 300);

            });

            //Act
            IList<IncomeItem>? incomeItems = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                incomeItems = await context.GetIncomeItems(category1, now.AddDays(-3), now.AddDays(-2));
            });

            //Assert
            Assert.IsNotNull(incomeItems);
            Assert.AreEqual(2, incomeItems!.Count);

            Assert.AreEqual(200, incomeItems[0].Amount);
            Assert.AreEqual("Income item 3", incomeItems[0].Description);

            Assert.AreEqual(300, incomeItems[1].Amount);
            Assert.AreEqual("Income 2 description", incomeItems[1].Description);
        }

        [TestMethod]
        public async Task CreateExpenseCategory_WithoutAccount_SetsAccountToDefaultAccount()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var account = new Account();
            var category = new ExpenseCategory();

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Accounts.Add(account);
                await context.SaveChangesAsync();
                await context.SetAsDefaultAsync(account);
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.Add(category);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var expenseCategory = await context.ExpenseCategories.FindAsync(category.ID);
                Assert.AreEqual(account.ID, expenseCategory.AccountID);
            });
        }

        [TestMethod]
        public async Task CreateExpenseCategory_AllowsNullDefaultAccount()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var category = new ExpenseCategory();

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.Add(category);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var expenseCategory = await context.ExpenseCategories.FindAsync(category.ID);
                Assert.AreEqual(null, expenseCategory.AccountID);
            });
        }

        [TestMethod]
        public async Task DeleteExpenseCategory_PostsNotification()
        {
            //Arrange
            var messenger = new WeakReferenceMessenger();
            var watcher = new MessageWatcher<ExpenseCategoryEvent>();
            var fixture = new BudgetDatabaseContext();
            fixture.Messenger.Register(watcher);

            var expenseCategory = new ExpenseCategory
            {
                Account = new Account()
            };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.Add(expenseCategory);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                var category = await context.ExpenseCategories.FindAsync(expenseCategory.ID);
                context.ExpenseCategories.Remove(category);
                await context.SaveChangesAsync();
            });

            //Assert
            ExpenseCategoryEvent? message = watcher.Messages.Last();
            Assert.AreEqual(expenseCategory.ID, message.ExpenseCategory.ID);
            Assert.AreEqual(EventType.Deleted, message.Type);
        }

        [TestMethod]
        public async Task CreateExpenseCategory_PostsNotification()
        {
            //Arrange
            var messenger = new WeakReferenceMessenger();
            var watcher = new MessageWatcher<ExpenseCategoryEvent>();
            var fixture = new BudgetDatabaseContext();
            fixture.Messenger.Register(watcher);

            var expenseCategory = new ExpenseCategory
            {
                Account = new Account()
            };

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.Add(expenseCategory);
                await context.SaveChangesAsync();
            });

            //Assert
            ExpenseCategoryEvent? message = watcher.Messages.Last();
            Assert.AreEqual(expenseCategory.ID, message.ExpenseCategory.ID);
            Assert.AreEqual(EventType.Created, message.Type);
        }

        [TestMethod]
        public async Task UpdateExpenseCategory_PostsNotification()
        {
            //Arrange
            var messenger = new WeakReferenceMessenger();
            var watcher = new MessageWatcher<ExpenseCategoryEvent>();
            var fixture = new BudgetDatabaseContext();
            fixture.Messenger.Register(watcher);

            var expenseCategory = new ExpenseCategory
            {
                Account = new Account()
            };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.ExpenseCategories.Add(expenseCategory);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                var category = await context.ExpenseCategories.FindAsync(expenseCategory.ID);
                category.CategoryName += "-Edited";
                await context.SaveChangesAsync();
            });

            //Assert
            ExpenseCategoryEvent? message = watcher.Messages.Last();
            Assert.AreEqual(expenseCategory.ID, message.ExpenseCategory.ID);
            Assert.AreEqual(EventType.Updated, message.Type);
        }

    }
}
