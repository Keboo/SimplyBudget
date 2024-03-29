﻿using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudgetShared.Data;

namespace SimplyBudgetSharedTests.Data;

[TestClass]
public class BudgetContextExtensionsTests
{
    [TestMethod]
    public async Task AddIncomeItem_UpdatesTotalAmount()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category1 = new ExpenseCategory { CurrentBalance = 20 };
        var category2 = new ExpenseCategory { CurrentBalance = 100 };
        var now = DateTime.Now;

        using var setupContext = factory.Create();
        setupContext.AddRange(category1, category2);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        await actContext.AddIncome("Test", now, (150, category1.ID), (50, category2.ID));

        //Assert
        using var assertContext = factory.Create();
        ExpenseCategoryItem? item = await assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync();
        Assert.AreEqual("Test", item.Description);
        Assert.AreEqual(now.Date, item.Date);

        Assert.AreEqual(2, item.Details?.Count);
        CollectionAssert.AreEquivalent(new[] { 150, 50 }, item.Details!.Select(x => x.Amount).ToList());

        var cat1 = await assertContext.ExpenseCategories.FindAsync(category1.ID);
        Assert.AreEqual(170, cat1?.CurrentBalance);

        var cat2 = await assertContext.ExpenseCategories.FindAsync(category2.ID);
        Assert.AreEqual(150, cat2?.CurrentBalance);
    }

    [TestMethod]
    public async Task AddIncomeItem_WithIgnoreBudget_UpdatesAllDetails()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory { CurrentBalance = 100 };
        var now = DateTime.Now;

        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        await actContext.AddIncome("Test", now, true, (50, category.ID));

        //Assert
        using var assertContext = factory.Create();
        ExpenseCategoryItem? item = await assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync();
        Assert.AreEqual(1, item.Details?.Count);
        Assert.IsTrue(item.IgnoreBudget);

        var cat1 = await assertContext.ExpenseCategories.FindAsync(category.ID);
        Assert.AreEqual(150, cat1?.CurrentBalance);
    }

    [TestMethod]
    public async Task AddTransaction_UpdatesTotalAmount()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category1 = new ExpenseCategory { CurrentBalance = 150 };
        var category2 = new ExpenseCategory { CurrentBalance = 100 };

        using var setupContext = factory.Create();
        setupContext.AddRange(category1, category2);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        await actContext.AddTransaction("transaction description", DateTime.Today, false, (80, category1.ID), (20, category2.ID));

        //Assert
        using var assertContext = factory.Create();
        var item = await assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleOrDefaultAsync();
        Assert.AreEqual(2, item?.Details!.Count);
        CollectionAssert.AreEquivalent(new[] { -80, -20 }, item!.Details!.Select(x => x.Amount).ToList());

        var cat1 = await assertContext.ExpenseCategories.FindAsync(category1.ID);
        Assert.AreEqual(70, cat1?.CurrentBalance);
        var cat2 = await assertContext.ExpenseCategories.FindAsync(category2.ID);
        Assert.AreEqual(80, cat2?.CurrentBalance);
    }

    [TestMethod]
    public async Task AddTransaction_WithIgnoreBudget_UpdatesAllDetails()
    {
        //Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory { CurrentBalance = 150 };

        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();

        //Act
        using var actContext = factory.Create();
        await actContext.AddTransaction("transaction description", DateTime.Today, true, (50, category.ID));

        //Assert
        using var assertContext = factory.Create();
        var item = await assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleOrDefaultAsync();
        Assert.AreEqual(1, item?.Details!.Count);
        Assert.IsTrue(item!.IgnoreBudget);

        var cat1 = await assertContext.ExpenseCategories.FindAsync(category.ID);
        Assert.AreEqual(100, cat1?.CurrentBalance);
    }

    [TestMethod]
    public async Task AddTransfer_MovesAmount()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category1 = new ExpenseCategory { CurrentBalance = 150 };
        var category2 = new ExpenseCategory { CurrentBalance = 150 };

        using var setupContext = factory.Create();
        setupContext.AddRange(category1, category2);
        await setupContext.SaveChangesAsync();
        var now = DateTime.Now;

        //Act
        using var actContext = factory.Create();
        await actContext.AddTransfer("Test", now, 50, category1, category2);

        //Assert
        using var assertContext = factory.Create();
        category1 = await assertContext.FindAsync<ExpenseCategory>(category1.ID);
        category2 = await assertContext.FindAsync<ExpenseCategory>(category2.ID);

        Assert.AreEqual(100, category1?.CurrentBalance);
        Assert.AreEqual(200, category2?.CurrentBalance);

        var transfer = await assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync();

        Assert.AreEqual(now.Date, transfer.Date);
        Assert.AreEqual("Test", transfer.Description);
        Assert.AreEqual(category1!.ID, transfer.Details?[0].ExpenseCategoryId);
        Assert.AreEqual(category2!.ID, transfer.Details?[1].ExpenseCategoryId);
        Assert.AreEqual(-50, transfer.Details?[0].Amount);
        Assert.AreEqual(50, transfer.Details?[1].Amount);
    }

    [TestMethod]
    public async Task AddTransfer_WithIgnoreBudget_UpdatesAllDetails()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category1 = new ExpenseCategory { CurrentBalance = 150 };
        var category2 = new ExpenseCategory { CurrentBalance = 150 };

        using var setupContext = factory.Create();
        setupContext.AddRange(category1, category2);
        await setupContext.SaveChangesAsync();

        var now = DateTime.Now;

        //Act
        using var actContext = factory.Create();
        await actContext.AddTransfer("Test", now, true, 50, category1, category2);

        //Assert
        using var assertContext = factory.Create();

        category1 = await assertContext.FindAsync<ExpenseCategory>(category1.ID);
        category2 = await assertContext.FindAsync<ExpenseCategory>(category2.ID);

        Assert.AreEqual(100, category1?.CurrentBalance);
        Assert.AreEqual(200, category2?.CurrentBalance);

        var transfer = await assertContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync();

        Assert.IsTrue(transfer.IgnoreBudget);
    }


    [TestMethod]
    public async Task GetCurrentAmount_ReturnsAmountInAccount()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var account1 = new Account();
        var account2 = new Account();

        var category1 = new ExpenseCategory { CurrentBalance = 100, Account = account1 };
        var category2 = new ExpenseCategory { CurrentBalance = 150, Account = account2 };
        var category3 = new ExpenseCategory { CurrentBalance = 200, Account = account1 };

        using var setupContext = factory.Create();
        setupContext.AddRange(category1, category2, category3);
        await setupContext.SaveChangesAsync();

        var now = DateTime.Now;

        //Act/Assert
        using var context = factory.Create();
        Assert.AreEqual(300, await context.GetCurrentAmount(account1.ID));
        Assert.AreEqual(150, await context.GetCurrentAmount(account2.ID));
    }

    [TestMethod]
    public async Task GetRemainingBudgetAmount_WithPercentage_ReturnsZero()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory
        {
            CurrentBalance = 100,
            BudgetedPercentage = 10
        };

        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();

        var now = DateTime.Now;

        //Act/Assert
        using var context = factory.Create();
        Assert.AreEqual(0, await context.GetRemainingBudgetAmount(category, now));
    }

    [TestMethod]
    public async Task GetRemainingBudgetAmount_ReturnsBudgettedAmount()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory
        {
            CurrentBalance = 100,
            BudgetedAmount = 50
        };

        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();

        var now = DateTime.Now;

        //Act/Assert
        using var context = factory.Create();
        Assert.AreEqual(50, await context.GetRemainingBudgetAmount(category, now));
    }

    [TestMethod]
    public async Task GetRemainingBudgetAmount_WithAllocation_ReturnsRemainingAmount()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory
        {
            CurrentBalance = 100,
            BudgetedAmount = 50
        };

        var now = DateTime.Now;
        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();
        await setupContext.AddIncome("", now, (10, category.ID));
        await setupContext.AddIncome("", now.AddMonths(1), (15, category.ID));
        await setupContext.AddIncome("", now.AddMonths(-1), (20, category.ID));

        //Act/Assert
        using var context = factory.Create();
        Assert.AreEqual(40, await context.GetRemainingBudgetAmount(category, now));
    }

    [TestMethod]
    [Description("Issue 10")]
    public async Task GetRemainingBudgetAmount_WithExpenseCategoryCap_ReturnsRemainingAmount()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory
        {
            CurrentBalance = 100,
            BudgetedAmount = 50,
            Cap = 120
        };

        var now = DateTime.Now;
        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();

        //Act/Assert
        using var context = factory.Create();
        Assert.AreEqual(20, await context.GetRemainingBudgetAmount(category, now));
    }

    [TestMethod]
    [Description("Issue 10")]
    public async Task GetRemainingBudgetAmount_WhenCurrentIsOverCap_ReturnsZero()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory
        {
            CurrentBalance = 100,
            BudgetedAmount = 50,
            Cap = 80
        };

        var now = DateTime.Now;
        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();

        //Act/Assert
        using var context = factory.Create();
        Assert.AreEqual(0, await context.GetRemainingBudgetAmount(category, now));
    }

    [TestMethod]
    [Description("Issue 29")]
    public async Task GetRemainingBudgetAmount_IgnoresNonBudgetItems()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var category = new ExpenseCategory
        {
            CurrentBalance = 100,
            BudgetedAmount = 50
        };

        var now = DateTime.Now;
        using var setupContext = factory.Create();
        setupContext.AddRange(category);
        await setupContext.SaveChangesAsync();
        var transaction = await setupContext.AddTransaction("Test", DateTime.Today, false, (20, category.ID));
        transaction.Details![0].IgnoreBudget = true;
        await setupContext.SaveChangesAsync();

        //Act/Assert
        using var context = factory.Create();
        Assert.AreEqual(50, await context.GetRemainingBudgetAmount(category, now));
    }
}
