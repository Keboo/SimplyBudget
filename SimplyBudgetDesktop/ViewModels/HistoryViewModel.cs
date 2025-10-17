using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using SimplyBudgetShared.Utilities;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SimplyBudget.ViewModels;

public class HistoryViewModel : CollectionViewModelBase<BudgetHistoryViewModel>,
    IRecipient<DatabaseEvent<ExpenseCategoryItem>>,
    IRecipient<DatabaseEvent<ExpenseCategoryItemDetail>>,
    IRecipient<CurrentMonthChanged>
{
    private IDataClient DataClient { get; }
    public ICurrentMonth CurrentMonth { get; }

    public IRelayCommand AddFilterCommand { get; }
    public ICommand DoSearchCommand { get; }
    public ICommand RemoveFilterCommand { get; }

    public ObservableCollection<Account> Accounts { get; } = [];

    public ObservableCollection<ExpenseCategory> ExpenseCategories { get; } = [];

    public ObservableCollection<ExpenseCategory> FilterCategories { get; } = [];

    private ExpenseCategory? _selectedCategory;
    public ExpenseCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                AddFilterCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private Account? _selectedAccount;
    public Account? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (SetProperty(ref _selectedAccount, value))
            {
                _ = LoadItemsAsync();
            }
        }
    }

    private string? _search;
    public string? Search
    {
        get => _search;
        set
        {
            if (SetProperty(ref _search, value))
            {
                SetFilterDisplay();
            }
        }
    }

    private string? _filterDisplay;
    public string? FilterDisplay
    {
        get => _filterDisplay;
        private set => SetProperty(ref _filterDisplay, value);
    }

    public HistoryViewModel(IDataClient dataClient, IMessenger messenger, ICurrentMonth currentMonth)
    {
        DataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
        CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));

        AddFilterCommand = new RelayCommand<ExpenseCategory>(OnAddFilter, x => x != null);
        RemoveFilterCommand = new RelayCommand<ExpenseCategory>(
            x => FilterCategories.Remove(x!), x => x != null);
        DoSearchCommand = new RelayCommand(OnDoSearch);

        FilterCategories.CollectionChanged += FilterCategories_CollectionChanged;

        messenger.Register<DatabaseEvent<ExpenseCategoryItem>>(this);
        messenger.Register<DatabaseEvent<ExpenseCategoryItemDetail>>(this);
        messenger.Register<CurrentMonthChanged>(this);
    }

    public override async Task LoadItemsAsync()
    {
        await base.LoadItemsAsync();
        ExpenseCategories.Clear();
        await foreach(var category in DataClient.ExpenseCategories.GetAllAsync())
        {
            ExpenseCategories.Add(category);
        }
    }

    private void OnAddFilter(ExpenseCategory? category)
    {
        if (category is null) return;
        if (!FilterCategories.Contains(category))
        {
            FilterCategories.Add(category);
        }
        SelectedCategory = null;
    }

    private async void OnDoSearch() => await LoadItemsAsync();

    private void SetFilterDisplay()
    {
        string rv = "";
        if (!string.IsNullOrEmpty(Search))
        {
            rv += $"\"{Search}\" ";
        }
        if (FilterCategories.Any())
        {
            rv += "in";
            foreach(var category in FilterCategories)
            {
                rv += $" {category.Name}";
            }
        }
        FilterDisplay = rv;
    }

    private void FilterCategories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = LoadItemsAsync();
        SetFilterDisplay();
    }

    protected override async IAsyncEnumerable<BudgetHistoryViewModel> GetItems()
    {
        var oldestTime = CurrentMonth.CurrenMonth.AddMonths(-12).StartOfMonth();

        int currentAccountAmount = 0;
        var categoryList = new List<int>();
        if (FilterCategories.Any())
        {
            categoryList.AddRange(FilterCategories.Select(x => x.ID));
        }
        else if (SelectedAccount?.ID is int selectedId)
        {
            currentAccountAmount = await DataClient.GetCurrentAmountAsync(selectedId, CancellationToken.None);
        }

        IDatasyncQueryable<ExpenseCategoryItem> query = DataClient.ExpenseCategoryItems.Query();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(x => EF.Functions.Like(x.Description!, $"%{Search}%"));
        }
        else
        {
            query = query.Where(x => x.Date >= oldestTime);
        }

        if (categoryList.Count != 0)
        {
            query = query.Where(x => x.Details!.Any(x => categoryList.Contains(x.ExpenseCategoryId)));
        }

        query = query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.ID);

        await foreach (var item in query.ToAsyncEnumerable())
        {
            yield return new BudgetHistoryViewModel(item, currentAccountAmount);
            if (!FilterCategories.Any() && SelectedAccount?.ID is int selectedId)
            {
                currentAccountAmount -= item.Details?
                    .Where(x => x.ExpenseCategory!.AccountID == selectedId)
                    .Sum(x => x.Amount) ?? 0;
            }
        }
    }

    protected override async Task ReloadItemsAsync()
    {
        int? selectedId = SelectedAccount?.ID;
        Accounts.Clear();
        await foreach(var account in DataClient.Accounts.GetAllAsync())
        {
            Accounts.Add(account);
        }
        _selectedAccount = Accounts.FirstOrDefault(x => x.ID == selectedId) 
            ?? await DataClient.GetDefaultAccountAsync();
        await base.ReloadItemsAsync();
        OnPropertyChanged(nameof(SelectedAccount));
    }

    public async Task DeleteItems(IEnumerable<BudgetHistoryViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (BudgetHistoryViewModel? item in items.ToList())
        {
            await item.Delete(DataClient);
        }
    }

    public async void Receive(DatabaseEvent<ExpenseCategoryItemDetail> message) => await LoadItemsAsync();

    public async void Receive(DatabaseEvent<ExpenseCategoryItem> message) => await LoadItemsAsync();

    public async void Receive(CurrentMonthChanged message) => await LoadItemsAsync();
}