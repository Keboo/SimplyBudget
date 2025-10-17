using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.Messaging;
using SimplyBudget.Properties;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels;

public partial class SettingsViewModel : CollectionViewModelBase<ExpenseCategoryRuleViewModel>
{
    private List<int> RemovedRuleIds { get; } = [];
    private IDataClient DataClient { get; }
    private IMessenger Messenger { get; }
    public ICommand OpenFolderCommand { get; }
    public ISnackbarMessageQueue MessageQueue { get; }
    public ObservableCollection<ExpenseCategory> ExpenseCategories { get; } = [];

    public SettingsViewModel(
        IDataClient dataClient, 
        ISnackbarMessageQueue messageQueue,
        IMessenger messenger)
    {
        DataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        OpenFolderCommand = new RelayCommand(OpenFolder);
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
        Settings.Default.StorageLocation = StorageLocation;
        Settings.Default.Save();

        var existingRules = await DataClient.ExpenseCategoryRules.GetAllAsync().ToListAsync();

        foreach (ExpenseCategoryRuleViewModel ruleViewModel in Items
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) &&
                        !string.IsNullOrWhiteSpace(x.RuleRegex)))
        {
            if (ruleViewModel.ExistingRuleId is { } ruleId)
            {
                if (existingRules.FirstOrDefault(x => x.ID == ruleId) is { } existingRule)
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
                await DataClient.ExpenseCategoryRules.AddAsync(rule);
            }
        }
        foreach(var ruleToRemove in RemovedRuleIds.Select(x => existingRules.Single(r => r.ID == x)))
        {
            await DataClient.ExpenseCategoryRules.RemoveAsync(ruleToRemove);
        }
        RemovedRuleIds.Clear();
        Messenger.Send(new StorageLocationChanged());
        MessageQueue.Enqueue("Saved");
        await ReloadItemsAsync();
    }

    [ObservableProperty]
    private string _StorageLocation = Settings.Default.StorageLocation;

    private void OpenFolder()
    {
        string targetPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(StorageLocation));
        Process.Start("explorer.exe", targetPath);
    }

    public override async Task LoadItemsAsync()
    {
        await base.LoadItemsAsync();
        ExpenseCategories.Clear();
        await foreach (var category in DataClient.ExpenseCategories.Query().OrderBy(x => x.Name).ToAsyncEnumerable())
        {
            ExpenseCategories.Add(category);
        }
    }

    protected override async IAsyncEnumerable<ExpenseCategoryRuleViewModel> GetItems()
    {
        RemovedRuleIds.Clear();
        await foreach (var rule in DataClient.ExpenseCategoryRules.GetAllAsync())
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
