using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetSharedTests;

namespace SimplyBudgetDesktop.Tests.ViewModels;

[ConstructorTests(typeof(SettingsViewModel))]
[TestClass]
public partial class SettingsViewModelTests
{
    [TestMethod]
    public async Task RefreshCommand_LoadsCategoryRules()
    {
        //Arrange
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var context = factory.Create();
        ExpenseCategory category1 = new() { Name = "Test Category" };
        ExpenseCategoryRule rule1 = new()
        {
            Name = "Match All",
            RuleRegex = ".*",
            ExpenseCategory = category1
        };
        context.ExpenseCategories.Add(category1);
        context.ExpenseCategoryRules.Add(rule1);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<SettingsViewModel>();

        //Act
        await vm.RefreshCommand.ExecuteAsync(null);

        //Assert
        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("Match All", vm.Items[0].Name);
        Assert.AreEqual(".*", vm.Items[0].RuleRegex);
        Assert.AreEqual(category1.ID, vm.Items[0].ExpenseCategoryId);
    }

    [TestMethod]
    public async Task NewExpenseCategoryRule_WithSave_SavesInTheDatabase()
    {
        //Arrange
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var context = factory.Create();
        ExpenseCategory category1 = new() { Name = "Test Category" };
        context.ExpenseCategories.Add(category1);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<SettingsViewModel>();

        //Act
        vm.Items.Add(new()
        {
            Name = "Match All",
            RuleRegex = ".*",
            ExpenseCategoryId = category1.ID
        });
        //NB: Saving twice to ensure that the rule is not duplicated
        await vm.SaveCommand.ExecuteAsync(null);
        await vm.SaveCommand.ExecuteAsync(null);

        //Assert
        using var assertContext = factory.Create();
        var foundRules = await assertContext.ExpenseCategoryRules.ToListAsync();
        Assert.AreEqual(1, foundRules.Count);
        Assert.AreEqual("Match All", foundRules[0].Name);
        Assert.AreEqual(".*", foundRules[0].RuleRegex);
        Assert.AreEqual(category1.ID, foundRules[0].ExpenseCategoryID);
    }

    [TestMethod]
    public async Task NewEmptyExpenseCategoryRule_WithSave_IsNotAdded()
    {
        //Arrange
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var context = factory.Create();
        ExpenseCategory category1 = new() { Name = "Test Category" };
        context.ExpenseCategories.Add(category1);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<SettingsViewModel>();

        //Act
        vm.Items.Add(new()
        {
            Name = "",
            RuleRegex = "",
            ExpenseCategoryId = category1.ID
        });
        //NB: Saving twice to ensure that the rule is not duplicated
        await vm.SaveCommand.ExecuteAsync(null);

        //Assert
        using var assertContext = factory.Create();
        var foundRules = await assertContext.ExpenseCategoryRules.ToListAsync();
        Assert.AreEqual(0, foundRules.Count);
    }

    [TestMethod]
    public async Task UpdateExistingExpenseCategoryRule_WithSave_SavesInTheDatabase()
    {
        //Arrange
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var context = factory.Create();
        ExpenseCategory category1 = new() { Name = "Test Category1" };
        ExpenseCategory category2 = new() { Name = "Test Category2" };
        ExpenseCategoryRule rule1 = new()
        {
            Name = "Match All",
            RuleRegex = ".*",
            ExpenseCategory = category1
        };
        context.ExpenseCategories.Add(category1);
        context.ExpenseCategories.Add(category2);
        context.ExpenseCategoryRules.Add(rule1);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<SettingsViewModel>();
        await vm.LoadItemsAsync();

        //Act
        var existingRule = vm.Items[0];
        existingRule.Name += "-Edit";
        existingRule.RuleRegex += "-Edit";
        existingRule.ExpenseCategoryId = category2.ID;
        await vm.SaveCommand.ExecuteAsync(null);

        //Assert
        using var assertContext = factory.Create();
        var foundRules = await assertContext.ExpenseCategoryRules.ToListAsync();
        Assert.AreEqual(1, foundRules.Count);
        Assert.AreEqual("Match All-Edit", foundRules[0].Name);
        Assert.AreEqual(".*-Edit", foundRules[0].RuleRegex);
        Assert.AreEqual(category2.ID, foundRules[0].ExpenseCategoryID);
    }

    [TestMethod]
    public async Task RemoveExistingExpenseCategoryRuleToCollection_WithSave_RemovesFromDatabase()
    {
        //Arrange
        var mocker = new AutoMocker().WithDefaults();
        using var factory = mocker.WithDbScope();
        using var context = factory.Create();
        ExpenseCategory category1 = new() { Name = "Test Category" };
        ExpenseCategoryRule rule1 = new()
        {
            Name = "Match All",
            RuleRegex = ".*",
            ExpenseCategory = category1
        };
        context.ExpenseCategories.Add(category1);
        context.ExpenseCategoryRules.Add(rule1);
        await context.SaveChangesAsync();

        var vm = mocker.CreateInstance<SettingsViewModel>();
        await vm.LoadItemsAsync();

        //Act
        vm.Items.RemoveAt(0);
        await vm.SaveCommand.ExecuteAsync(null);

        //Assert
        using var assertContext = factory.Create();
        var foundRules = await assertContext.ExpenseCategoryRules.ToListAsync();
        Assert.AreEqual(0, foundRules.Count);
    }
}
