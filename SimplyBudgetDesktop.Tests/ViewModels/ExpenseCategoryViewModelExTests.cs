using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetSharedTests;

namespace SimplyBudgetDesktop.Tests.ViewModels;

[TestClass]
public class ExpenseCategoryViewModelExTests
{
    [TestMethod]
    public async Task Create_SpecifyingMonth_LoadsData()
    {
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();
        var expenseCategory = new ExpenseCategory();
        var now = DateTime.Now;

        var twoMonthsAgo = now.AddMonths(-2);
        var lastMonth = now.AddMonths(-1);

        var setupContext = factory.Create();
        setupContext.AddRange(expenseCategory);
        await setupContext.SaveChangesAsync();

        await setupContext.AddTransaction(expenseCategory.ID, 100, "Transaction 1", twoMonthsAgo);
        await setupContext.AddIncome("Income 1", twoMonthsAgo, (300, expenseCategory.ID));

        await setupContext.AddTransaction(expenseCategory.ID, 200, "Transaction 2", lastMonth);
        await setupContext.AddIncome("Income 2", lastMonth, (300, expenseCategory.ID));

        await setupContext.AddTransaction(expenseCategory.ID, 300, "Transaction 3", now);
        await setupContext.AddIncome("Income 3", now, (300, expenseCategory.ID));


        var vm = await ExpenseCategoryViewModelEx.Create(() => factory.Create(), expenseCategory, lastMonth);

        // Balance always shows current even if we query a previous month
        Assert.AreEqual(300, vm.Balance);
        Assert.AreEqual(300, vm.MonthlyAllocations);
        Assert.AreEqual(200, vm.MonthlyExpenses);
    }

    [TestMethod]
    public async Task Create_WithIgnoreBudgetItems_DoesNotCountThemInTotal()
    {
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();
        var expenseCategory1 = new ExpenseCategory();
        var expenseCategory2 = new ExpenseCategory();
        var now = DateTime.Now;

        using var setupContext = factory.Create();

        setupContext.AddRange(expenseCategory1, expenseCategory2);
        await setupContext.SaveChangesAsync();

        await setupContext.AddIncome("Income 1", now, false, (300, expenseCategory1.ID));
        await setupContext.AddIncome("Income 2", now, true, (200, expenseCategory1.ID));

        await setupContext.AddTransaction("Transaction 1", now, false, (100, expenseCategory1.ID));
        await setupContext.AddTransaction("Transaction 2", now, true, (150, expenseCategory1.ID));

        await setupContext.AddTransfer("Transfer 1", now, false, 50, expenseCategory1, expenseCategory2);
        await setupContext.AddTransfer("Transfer 2", now, true, 25, expenseCategory1, expenseCategory2);


        var vm1 = await ExpenseCategoryViewModelEx.Create(() => factory.Create(), expenseCategory1, now);
        var vm2 = await ExpenseCategoryViewModelEx.Create(() => factory.Create(), expenseCategory2, now);

        // Balance always shows current even for items that ignore budget
        Assert.AreEqual(175, vm1.Balance); //(Income 300 + 200) - (Transaction 100 + 150) - (Transfer 50 + 25)
        Assert.AreEqual(250, vm1.MonthlyAllocations); //Income 300 - Transfer Out 50
        Assert.AreEqual(100, vm1.MonthlyExpenses); //Transaction 100

        Assert.AreEqual(75, vm2.Balance); //Transfer 50 + Transfer 25
        Assert.AreEqual(50, vm2.MonthlyAllocations); //Transfer 50
        Assert.AreEqual(0, vm2.MonthlyExpenses);
    }
}
