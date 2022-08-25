using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetSharedTests;

namespace SimplyBudgetDesktop.Tests.ViewModels;

[TestClass]
public class AddItemViewModelTests
{
    [TestMethod]
    public async Task SelectedType_Transaction_LoadsItems()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var context = factory.Create();
        var category1 = new ExpenseCategory { Name = "Foo" };
        var category2 = new ExpenseCategory { Name = "Bar" };
        context.AddRange(category1, category2);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.SelectedType = AddType.Transaction;

        Assert.AreEqual(1, vm.LineItems.Count);
        Assert.IsNull(vm.LineItems[0].SelectedCategory);
    }

    [TestMethod]
    public async Task SelectedType_Income_LoadsItems()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();

        using var context = factory.Create();
        var category1 = new ExpenseCategory { Name = "Foo" };
        var category2 = new ExpenseCategory { Name = "Bar" };
        var category3 = new ExpenseCategory { Name = "Cat" };
        var category4 = new ExpenseCategory { Name = "Hidden", IsHidden = true };
        context.AddRange(category1, category2, category3, category4);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.SelectedType = AddType.Income;

        Assert.AreEqual(3, vm.LineItems.Count);
        Assert.AreEqual(category2, vm.LineItems[0].SelectedCategory);
        Assert.AreEqual(category3, vm.LineItems[1].SelectedCategory);
        Assert.AreEqual(category1, vm.LineItems[2].SelectedCategory);
    }

    [TestMethod]
    public async Task SelectedType_Transfer_LoadsItems()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var _ = mocker.WithAutoDIResolver();

        using var context = factory.Create();
        var category1 = new ExpenseCategory { Name = "Foo" };
        var category2 = new ExpenseCategory { Name = "Bar" };
        var category3 = new ExpenseCategory { Name = "Cat" };
        context.AddRange(category1, category2, category3);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.SelectedType = AddType.Transfer;

        Assert.AreEqual(2, vm.LineItems.Count);
        Assert.IsNull(vm.LineItems[0].SelectedCategory);
        Assert.IsNull(vm.LineItems[1].SelectedCategory);
        Assert.AreEqual(DateTime.Today, vm.Date);
    }

    [TestMethod]
    public void RemovingItem_UpdatesRemainingAmount()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var _ = mocker.WithDbScope();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.SelectedType = AddType.Transaction;
        vm.AddItemCommand.Execute(null);
        vm.LineItems[0].Amount = 300;
        vm.LineItems[1].Amount = 400;

        vm.RemoveItemCommand.Execute(vm.LineItems[0]);

        Assert.AreEqual(400, vm.TotalAmount);
    }

    [TestMethod]
    public async Task AutoAllocateCommand_AllocatesIncomeAmount()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();

        await using var context = factory.Create();
        var category1 = new ExpenseCategory { Name = "Bar", BudgetedPercentage = 10 };
        var category2 = new ExpenseCategory { Name = "Cat", BudgetedAmount = 100 };
        var category3 = new ExpenseCategory { Name = "Foo", BudgetedAmount = 200 };
        context.AddRange(category1, category2, category3);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.SelectedType = AddType.Income;
        vm.TotalAmount = 200;

        vm.AutoAllocateCommand.Execute(null);

        Assert.AreEqual(3, vm.LineItems.Count);
        Assert.AreEqual(20, vm.LineItems[0].Amount);
        Assert.AreEqual(100, vm.LineItems[1].Amount);
        Assert.AreEqual(80, vm.LineItems[2].Amount);
    }

    [TestMethod]
    [DataRow(-3)]
    [DataRow(2)]
    public void Date_MonthsOutOfRange_ShowsWarning(int monthOffset)
    {
        var now = DateTime.Now;
        var mocker = new AutoMocker().WithDefaults();
        using var __ = mocker.WithAutoDIResolver();
        using var _ = mocker.WithDbScope();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.Date = DateTime.Now.AddMonths(monthOffset);

        var errors = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual($"Date should be between {now.AddMonths(-2).StartOfMonth():d} and {now.AddMonths(1).EndOfMonth():d}", errors[0]);
    }

    [TestMethod]
    [DataRow(-2)]
    [DataRow(0)]
    [DataRow(1)]
    public void Date_MonthsWithinRange_DoesNotShowWarning(int monthOffset)
    {
        var mocker = new AutoMocker().WithDefaults();
        using var __ = mocker.WithAutoDIResolver();
        using var _ = mocker.WithDbScope();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.Date = DateTime.Now.AddMonths(monthOffset);

        var errors = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    [Description("Issue 14")]
    public void Date_ChangingCurrentMonth_ClearsWarning()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var _ = mocker.WithDbScope();
        using var __ = mocker.WithAutoDIResolver();
        using var ___ = mocker.WithSynchonousTaskRunner();

        ICurrentMonth current = mocker.Get<ICurrentMonth>();

        var vm = mocker.CreateInstance<AddItemViewModel>();
        current.CurrenMonth = DateTime.Now.AddMonths(-10);

        vm.Date = DateTime.Now;
        var errorsBefore = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();
        current.CurrenMonth = DateTime.Now;
        var errorsAfter = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();

        Assert.AreEqual(1, errorsBefore.Count);
        Assert.AreEqual(0, errorsAfter.Count);
    }

    [TestMethod]
    public void TotalAmount_Transaction_UpdatesRemainingAmount()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var _ = mocker.WithDbScope();
        using var __ = mocker.WithAutoDIResolver();
        using var ___ = mocker.WithSynchonousTaskRunner();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        vm.SelectedType = AddType.Transaction;
        vm.AddItemCommand.Execute(null);
        vm.TotalAmount = 100_00;

        vm.LineItems[0].Amount = 25_00;

        Assert.AreEqual(2, vm.LineItems.Count);
        Assert.AreEqual(25_00, vm.LineItems[0].Amount);
        Assert.AreEqual(0, vm.LineItems[1].Amount);
        Assert.AreEqual(75_00, vm.RemainingAmount);
    }

    [TestMethod]
    public async Task SubmitCommand_TransactionIgnoreBudget_CreatesTransaction()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var _ = mocker.WithAutoDIResolver();
        using var __ = mocker.WithSynchonousTaskRunner();
        using var context = factory.Create();

        var category = new ExpenseCategory();
        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        var today = DateTime.Today;
        vm.SelectedType = AddType.Transaction;
        vm.Description = "Test description";
        vm.Date = today;
        vm.LineItems[0].Amount = 25_00;
        vm.LineItems[0].SelectedCategory = category;

        await vm.SubmitCommand.ExecuteAsync(true);

        using var verificationContext = factory.Create();
        ExpenseCategoryItem transaction = await verificationContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync();

        Assert.AreEqual("Test description", transaction.Description);
        Assert.AreEqual(today, transaction.Date);
        Assert.IsTrue(transaction.IgnoreBudget);
        Assert.AreEqual(1, transaction.Details?.Count);
        Assert.AreEqual(-25_00, transaction.Details![0].Amount);
        Assert.AreEqual(category.ID, transaction.Details![0].ExpenseCategoryId);
    }

    [TestMethod]
    public async Task SubmitCommand_IncomeIgnoreBudget_CreatesIncome()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var _ = mocker.WithAutoDIResolver();
        using var __ = mocker.WithSynchonousTaskRunner();
        using var context = factory.Create();

        var category = new ExpenseCategory();
        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        var today = DateTime.Today;
        vm.SelectedType = AddType.Income;
        vm.Description = "Test description";
        vm.Date = today;
        vm.TotalAmount = 25_00;
        vm.LineItems[0].Amount = 25_00;
        vm.LineItems[0].SelectedCategory = category;

        await vm.SubmitCommand.ExecuteAsync(true);

        using var verfictionContext = factory.Create();
        ExpenseCategoryItem income = await verfictionContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync();

        Assert.AreEqual("Test description", income.Description);
        Assert.AreEqual(today, income.Date);
        Assert.IsTrue(income.IgnoreBudget);
        Assert.AreEqual(1, income.Details?.Count);
        Assert.AreEqual(25_00, income.Details![0].Amount);
        Assert.AreEqual(category.ID, income.Details![0].ExpenseCategoryId);
    }

    [TestMethod]
    public async Task SubmitCommand_TransferIgnoreBudget_CreatesTransfer()
    {
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var _ = mocker.WithAutoDIResolver();
        using var __ = mocker.WithSynchonousTaskRunner();
        using var context = factory.Create();

        var category1 = new ExpenseCategory();
        var category2 = new ExpenseCategory();
        context.ExpenseCategories.Add(category1);
        context.ExpenseCategories.Add(category2);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<AddItemViewModel>();

        var today = DateTime.Today;
        vm.SelectedType = AddType.Transfer;
        vm.Description = "Test description";
        vm.Date = today;
        vm.TotalAmount = 25_00;
        vm.LineItems[0].SelectedCategory = category1;
        vm.LineItems[1].SelectedCategory = category2;

        await vm.SubmitCommand.ExecuteAsync(true);

        using var verificationContext = factory.Create();
        ExpenseCategoryItem transfer = await verificationContext.ExpenseCategoryItems
            .Include(x => x.Details)
            .SingleAsync();

        Assert.IsTrue(transfer.IsTransfer);
        Assert.AreEqual("Test description", transfer.Description);
        Assert.AreEqual(today, transfer.Date);
        Assert.IsTrue(transfer.IgnoreBudget);
        Assert.AreEqual(2, transfer.Details?.Count);
        Assert.AreEqual(-25_00, transfer.Details![0].Amount);
        Assert.AreEqual(category1.ID, transfer.Details![0].ExpenseCategoryId);
        Assert.AreEqual(25_00, transfer.Details![1].Amount);
        Assert.AreEqual(category2.ID, transfer.Details![1].ExpenseCategoryId);
    }
}
