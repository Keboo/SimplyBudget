using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.Messaging;
using SimplyBudget.Properties;
using SimplyBudgetShared.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels;

public partial class SettingsViewModel : CollectionViewModelBase<ExpenseCategoryRuleViewModel>
{
    private List<int> RemovedRuleIds { get; } = new();
    private Func<BudgetContext> ContextFactory { get; }
    private IMessenger Messenger { get; }
    public ISnackbarMessageQueue MessageQueue { get; }
    public ObservableCollection<ExpenseCategory> ExpenseCategories { get; } = new();

    public SettingsViewModel(
        Func<BudgetContext> contextFactory, 
        ISnackbarMessageQueue messageQueue,
        IMessenger messenger)
    {
        ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        BindingOperations.EnableCollectionSynchronization(ExpenseCategories, new());
    }

    [RelayCommand]
    private void OnRemoveRule(ExpenseCategoryRuleViewModel? rule)
    {
        if (rule is null) return;
        Items.Remove(rule);
        if (rule.ExistingRuleId is { } ruleId)
        {
            RemovedRuleIds.Add(ruleId);
        }
    }

    [RelayCommand]
    private async Task OnSave()
    {
        using var context = ContextFactory();
        foreach (ExpenseCategoryRuleViewModel ruleViewModel in Items
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) &&
                        !string.IsNullOrWhiteSpace(x.RuleRegex)))
        {
            if (ruleViewModel.ExistingRuleId is { } ruleId)
            {
                if (await context.FindAsync<ExpenseCategoryRule>(ruleId) is { } existingRule)
                {
                    existingRule.Name = ruleViewModel.Name;
                    existingRule.RuleRegex = ruleViewModel.RuleRegex;
                    existingRule.ExpenseCategoryID = ruleViewModel.ExpenseCategoryId;
                }
                //TODO Else?
            }
            else
            {
                var rule = new ExpenseCategoryRule
                {
                    Name = ruleViewModel.Name,
                    RuleRegex = ruleViewModel.RuleRegex,
                    ExpenseCategoryID = ruleViewModel.ExpenseCategoryId
                };
                context.ExpenseCategoryRules.Add(rule);
            }
        }
        foreach(var ruleIdToRemove in RemovedRuleIds)
        {
            if (await context.ExpenseCategoryRules.FindAsync(ruleIdToRemove) is { } existingRule)
            {
                context.ExpenseCategoryRules.Remove(existingRule);
            }
        }
        await context.SaveChangesAsync();
        RemovedRuleIds.Clear();
        Messenger.Send(new StorageLocationChanged());
        MessageQueue.Enqueue("Saved");
        await ReloadItemsAsync();
    }

    public override async Task LoadItemsAsync()
    {
        await base.LoadItemsAsync();
        using var context = ContextFactory();
        ExpenseCategories.Clear();
        await foreach (var category in context.ExpenseCategories.OrderBy(x => x.Name).AsAsyncEnumerable())
        {
            ExpenseCategories.Add(category);
        }
    }

    protected override async IAsyncEnumerable<ExpenseCategoryRuleViewModel> GetItems()
    {
        RemovedRuleIds.Clear();
        using var context = ContextFactory();
        await foreach (var rule in context.ExpenseCategoryRules.AsAsyncEnumerable())
        {
            yield return new ExpenseCategoryRuleViewModel(rule);
        }
    }

    protected override void ItemRemoved(ExpenseCategoryRuleViewModel item)
    {
        base.ItemRemoved(item);
        if (item.ExistingRuleId is { } ruleId)
        {
            RemovedRuleIds.Add(ruleId);
        }
    }
}
