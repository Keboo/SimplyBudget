using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.Messaging;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetSharedTests;

namespace SimplyBudgetDesktop.Tests.ViewModels;

[TestClass]
public class MainWindowViewModelTests
{
    [TestMethod]
    public void Constructor_CreatesDependencies()
    {
        var mocker = new AutoMocker()
            .WithMessenger()
            .WithCurrentMonth();
        using var scope = mocker.WithDbScope();

        var vm = mocker.CreateInstance<MainWindowViewModel>();
        Assert.IsNotNull(vm.Budget);
        Assert.IsNotNull(vm.History);
        Assert.IsNotNull(vm.Accounts);
    }

    [TestMethod]
    [DataRow(null, AddType.Transaction)]
    [DataRow(AddType.None, AddType.Transaction)]
    [DataRow(AddType.Income, AddType.Income)]
    [DataRow(AddType.Transaction, AddType.Transaction)]
    [DataRow(AddType.Transfer, AddType.Transfer)]
    public void AddCommands_SetsAddItem(AddType? parameter, AddType expected)
    {
        var mocker = new AutoMocker()
            .WithDefaults();
        using var scope = mocker.WithDbScope();
        using var _ = mocker.WithAutoDIResolver();

        var vm = mocker.CreateInstance<MainWindowViewModel>();

        Assert.IsTrue(vm.ShowAddCommand.CanExecute(parameter));
        vm.ShowAddCommand.Execute(parameter);

        Assert.IsNotNull(vm.AddItem);
        Assert.AreEqual(expected, vm.AddItem?.SelectedType);
    }

    [TestMethod]
    public void OnDoneAddingItemMessage_AddItemCleared()
    {
        var mocker = new AutoMocker()
            .WithMessenger()
            .WithCurrentMonth();
        using var scope = mocker.WithDbScope();

        var vm = mocker.CreateInstance<MainWindowViewModel>();
        vm.AddItem = mocker.CreateInstance<AddItemViewModel>();

        IMessenger messenger = mocker.Get<IMessenger>();
        messenger.Send(new DoneAddingItemMessage());

        Assert.IsNull(vm.AddItem);
    }

    [TestMethod]
    public async Task OnAddNewItem_MatchingRule_SetsCategory()
    {
        var mocker = new AutoMocker()
            .WithMessenger()
            .WithCurrentMonth();
        using var _ = mocker.WithAutoDIResolver();
        using var scope = mocker.WithDbScope();
        int categoryId;
        using (var context = scope.Create())
        {
            ExpenseCategory cellCategory = new() { Name = "Cell Category" };
            context.ExpenseCategories.Add(cellCategory);

            context.ExpenseCategoryRules.Add(new()
            {
                Name = "Cell Rule",
                RuleRegex = "Cell",
                ExpenseCategory = cellCategory
            });
            await context.SaveChangesAsync();
            categoryId = cellCategory.ID;
        }

        DateTime today = DateTime.Today;
        var vm = mocker.CreateInstance<MainWindowViewModel>();

        IMessenger messenger = mocker.Get<IMessenger>();

        messenger.Send(new AddItemMessage(AddType.Transaction, today, "My Cell Phone", new[]
        {
            new LineItem(500)
        }));

        Assert.IsNotNull(vm.AddItem);
        Assert.AreEqual(AddType.Transaction, vm.AddItem.SelectedType);
        Assert.AreEqual(today, vm.AddItem.Date);
        Assert.AreEqual("My Cell Phone", vm.AddItem.Description);
        Assert.AreEqual(1, vm.AddItem.LineItems.Count);
        Assert.AreEqual(500, vm.AddItem.LineItems[0].Amount);
        Assert.AreEqual(categoryId, vm.AddItem.LineItems[0].SelectedCategory?.ID);
    }
}
