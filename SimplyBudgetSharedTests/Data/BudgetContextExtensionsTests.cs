using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Linq;
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

            var category1 = new ExpenseCategory { CurrentBalance = 20 };
            var category2 = new ExpenseCategory { CurrentBalance = 100 };
            var now = DateTime.Now;

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category1, category2);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddIncome("Test", now, (150, category1.ID), (50, category2.ID));
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                ExpenseCategoryItem? item = await context.ExpenseCategoryItems
                    .Include(x => x.Details)
                    .SingleAsync();
                Assert.AreEqual("Test", item.Description);
                Assert.AreEqual(now.Date, item.Date);

                Assert.AreEqual(2, item.Details?.Count);
                CollectionAssert.AreEquivalent(new[] { 150, 50 }, item.Details.Select(x => x.Amount).ToList());

                var cat1 = await context.ExpenseCategories.FindAsync(category1.ID);
                Assert.AreEqual(170, cat1.CurrentBalance);

                var cat2 = await context.ExpenseCategories.FindAsync(category2.ID);
                Assert.AreEqual(150, cat2.CurrentBalance);
            });
        }

        [TestMethod]
        public async Task AddTransaction_UpdatesTotalAmount()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            var category1 = new ExpenseCategory { CurrentBalance = 150 };
            var category2 = new ExpenseCategory { CurrentBalance = 100 };

            await fixture.PerformDatabaseOperation(async context =>
            {
                context.AddRange(category1, category2);
                await context.SaveChangesAsync();
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                await context.AddTransaction("transaction description", DateTime.Today, (80, category1.ID), (20, category2.ID));
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                var item = await context.ExpenseCategoryItems
                    .Include(x => x.Details)
                    .SingleOrDefaultAsync();
                Assert.AreEqual(2, item.Details!.Count);
                CollectionAssert.AreEquivalent(new[] { -80, -20 }, item.Details.Select(x => x.Amount).ToList());

                var cat1 = await context.ExpenseCategories.FindAsync(category1.ID);
                Assert.AreEqual(70, cat1.CurrentBalance);
                var cat2 = await context.ExpenseCategories.FindAsync(category2.ID);
                Assert.AreEqual(80, cat2.CurrentBalance);
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

                var transfer = await context.ExpenseCategoryItems
                    .Include(x => x.Details)
                    .SingleAsync();
                
                Assert.AreEqual(now.Date, transfer.Date);
                Assert.AreEqual("Test", transfer.Description);
                Assert.AreEqual(category1.ID, transfer.Details?[0].ExpenseCategoryId);
                Assert.AreEqual(category2.ID, transfer.Details?[1].ExpenseCategoryId);
                Assert.AreEqual(-50, transfer.Details?[0].Amount);
                Assert.AreEqual(50, transfer.Details?[1].Amount);
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
