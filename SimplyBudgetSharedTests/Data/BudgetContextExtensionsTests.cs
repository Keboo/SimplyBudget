﻿using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class BudgetContextExtensionsTests
    {
        [TestMethod]
        public async Task AddIncomeItem_UpdatesTotalAmount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            var expenseCategory = new ExpenseCategory { CurrentBalance = 20 };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(expenseCategory);
                await context.SaveChangesAsync();
            });
            var now = DateTime.Now;


            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddIncome("Test", now, (150, expenseCategory.ID));
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var income = await context.Incomes.SingleAsync();
                Assert.AreEqual("Test", income.Description);
                Assert.AreEqual(now.Date, income.Date);
                Assert.AreEqual(150, income.TotalAmount);

                var incomeItem = await context.IncomeItems.SingleAsync(x => x.IncomeID == income.ID);
                Assert.AreEqual(150, incomeItem.Amount);
                var category = await context.ExpenseCategories.FindAsync(expenseCategory.ID);
                Assert.AreEqual(170, category.CurrentBalance);
            });
        }

        [TestMethod]
        public async Task AddTransaction_UpdatesTotalAmount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            var expenseCategory = new ExpenseCategory { CurrentBalance = 150 };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.Add(expenseCategory);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddTransaction(expenseCategory.ID, 80, "transaction description");
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var transaction = await context.TransactionItems.SingleAsync(x => x.ExpenseCategoryID == expenseCategory.ID);
                Assert.AreEqual(80, transaction.Amount);
                var category = await context.ExpenseCategories.FindAsync(expenseCategory.ID);
                Assert.AreEqual(70, category.CurrentBalance);
            });
        }

        [TestMethod]
        public async Task AddTransfer_MovesAmount()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var category1 = new ExpenseCategory { CurrentBalance = 150 };
            var category2 = new ExpenseCategory { CurrentBalance = 150 };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category1, category2);
                await context.SaveChangesAsync();
            });
            var now = DateTime.Now;

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddTransfer("Test", now, 50, category1, category2);
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                category1 = await context.FindAsync<ExpenseCategory>(category1.ID);
                category2 = await context.FindAsync<ExpenseCategory>(category2.ID);

                Assert.AreEqual(100, category1.CurrentBalance);
                Assert.AreEqual(200, category2.CurrentBalance);

                var transfer = await context.Transfers.SingleAsync();
                Assert.AreEqual(now.Date, transfer.Date);
                Assert.AreEqual(50, transfer.Amount);
                Assert.AreEqual("Test", transfer.Description);
                Assert.AreEqual(category1.ID, transfer.FromExpenseCategoryID);
                Assert.AreEqual(category2.ID, transfer.ToExpenseCategoryID);
            });
        }

        [TestMethod]
        public async Task GetCurrentAmount_ReturnsAmountInAccount()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var account1 = new Account();
            var account2 = new Account();

            var category1 = new ExpenseCategory { CurrentBalance = 100, Account = account1 };
            var category2 = new ExpenseCategory { CurrentBalance = 150, Account = account2 };
            var category3 = new ExpenseCategory { CurrentBalance = 200, Account = account1 };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category1, category2, category3);
                await context.SaveChangesAsync();
            });
            var now = DateTime.Now;

            //Act/Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.AreEqual(300, await context.GetCurrentAmount(account1.ID));
                Assert.AreEqual(150, await context.GetCurrentAmount(account2.ID));
            });
        }

        [TestMethod]
        public async Task GetRemainingBudgetAmount_WithPercentage_ReturnsZero()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var category = new ExpenseCategory
            {
                CurrentBalance = 100,
                BudgetedPercentage = 10
            };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category);
                await context.SaveChangesAsync();
            });
            var now = DateTime.Now;

            //Act/Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.AreEqual(0, await context.GetRemainingBudgetAmount(category, now));
            });
        }

        [TestMethod]
        public async Task GetRemainingBudgetAmount_ReturnsBudgettedAmount()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var category = new ExpenseCategory
            {
                CurrentBalance = 100,
                BudgetedAmount = 50
            };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category);
                await context.SaveChangesAsync();
            });
            var now = DateTime.Now;

            //Act/Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.AreEqual(50, await context.GetRemainingBudgetAmount(category, now));
            });
        }

        [TestMethod]
        public async Task GetRemainingBudgetAmount_WithAllocation_ReturnsRemainingAmount()
        {
            // Arrange
            var fixture = new BudgetDatabaseContext();

            var category = new ExpenseCategory
            {
                CurrentBalance = 100,
                BudgetedAmount = 50
            };

            var now = DateTime.Now;

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category);
                await context.SaveChangesAsync();
                await context.AddIncome("", now, (10, category.ID));
                await context.AddIncome("", now.AddMonths(1), (15, category.ID));
                await context.AddIncome("", now.AddMonths(-1), (20, category.ID));
            });

            //Act/Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.AreEqual(40, await context.GetRemainingBudgetAmount(category, now));
            });
        }
    }
}
