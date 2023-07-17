using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using SimplyBudgetShared.Utilities;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels;

public class BudgetViewModel : CollectionViewModelBase<ExpenseCategoryViewModelEx>,
    IRecipient<DatabaseEvent<ExpenseCategory>>,
    IRecipient<CurrentMonthChanged>
{
    private Func<BudgetContext> ContextFactory { get; }

    public IMessenger Messenger { get; }
    public ICurrentMonth CurrentMonth { get; }
    public ICommand DoSearchCommand { get; }

    public BudgetViewModel(Func<BudgetContext> contextFactory, IMessenger messenger, ICurrentMonth currentMonth)
    {
        ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
        messenger.Register<DatabaseEvent<ExpenseCategory>>(this);
        messenger.Register<CurrentMonthChanged>(this);
        DoSearchCommand = new RelayCommand(OnDoSearch);
        GroupItems = true;
    }

    private int _totalBudget;
    public int TotalBudget
    {
        get => _totalBudget;
        private set => SetProperty(ref _totalBudget, value);
    }

    public ICollectionView ExpenseCategoriesView
    {
        get
        {
            SetDescriptors();
            return _view;
        }
    }

    public string Title => "Budget for " + DateTime.Today.ToString("MMMM");

    private bool _groupItems;
    public bool GroupItems
    {
        get => _groupItems;
        set
        {
            if (SetProperty(ref _groupItems, value))
            {
                SetDescriptors();
            }
        }
    }

    private bool _showAll;
    public bool ShowAll
    {
        get => _showAll;
        set
        {
            if (SetProperty(ref _showAll, value))
            {
                SetDescriptors();
            }
        }
    }

    private string? _search;
    public string? Search
    {
        get => _search;
        set
        {
            SetProperty(ref _search, value);
        }
    }

    protected override async IAsyncEnumerable<ExpenseCategoryViewModelEx> GetItems()
    {
        using var context = ContextFactory();
        await foreach (var category in context.ExpenseCategories)
        {
            yield return await ExpenseCategoryViewModelEx.Create(ContextFactory, category, CurrentMonth.CurrenMonth);
        }
    }

    public async void Receive(DatabaseEvent<ExpenseCategory> @event)
    {
        var expenseCategory = @event.Item;

        switch (@event.Type)
        {
            case EventType.Created:
                Items.Add(await ExpenseCategoryViewModelEx.Create(ContextFactory, expenseCategory, CurrentMonth.CurrenMonth));
                break;
            case EventType.Updated:
                Items.RemoveFirst(x => x.ExpenseCategoryID == expenseCategory.ID);
                Items.Add(await ExpenseCategoryViewModelEx.Create(ContextFactory, expenseCategory, CurrentMonth.CurrenMonth));
                break;
            case EventType.Deleted:
                Items.RemoveFirst(x => x.ExpenseCategoryID == expenseCategory.ID);
                break;
        }
    }

    public void OpenExpenseCategory(ExpenseCategoryViewModelEx category)
        => Messenger.Send(new OpenHistory(category));

    public async Task<bool> SaveChanges(ExpenseCategoryViewModelEx category)
    {
        using var context = ContextFactory();
        if (await context.ExpenseCategories.FirstOrDefaultAsync(x => x.ID == category.ExpenseCategoryID) is ExpenseCategory dbCategory)
        {
            dbCategory.Name = category.EditingName;
            dbCategory.CategoryName = category.EditingCategory;
            dbCategory.BudgetedPercentage = !category.EditIsAmountType ? category.EditAmount : 0;
            dbCategory.BudgetedAmount = category.EditIsAmountType ? category.EditAmount : 0;
            dbCategory.Cap = category.EditingCap;
            dbCategory.AccountID = category.Account?.ID;
            await context.SaveChangesAsync();
            category.Name = dbCategory.Name;
            category.CategoryName = dbCategory.CategoryName;
            category.BudgetedAmount = dbCategory.BudgetedAmount;
            category.BudgetedPercentage = dbCategory.BudgetedPercentage;
            category.Cap = dbCategory.Cap;
            category.Account = dbCategory.Account;
            return true;
        }
        return false;
    }

    public async Task Delete(ExpenseCategoryViewModelEx category)
    {
        using var context = ContextFactory();
        if (context.ExpenseCategories.Find(category.ExpenseCategoryID) is ExpenseCategory dbCategory)
        {
            dbCategory.IsHidden = true;
            await context.SaveChangesAsync();
            category.IsHidden = true;
        }
    }

    public async Task Undelete(ExpenseCategoryViewModelEx category)
    {
        using var context = ContextFactory();

        if (context.ExpenseCategories.Find(category.ExpenseCategoryID) is ExpenseCategory dbCategory)
        {
            dbCategory.IsHidden = false;
            await context.SaveChangesAsync();
            category.IsHidden = false;
        }
    }

    public async void Receive(CurrentMonthChanged message)
        => await LoadItemsAsync();

    private void OnDoSearch() => SetDescriptors();

    private void SetDescriptors()
    {
        _view.SortDescriptions.Clear();
        if (GroupItems)
            _view.SortDescriptions.Add(new SortDescription("CategoryName", ListSortDirection.Ascending));
        _view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

        _view.Filter = x =>
        {
            if (x is ExpenseCategoryViewModelEx vm)
            {
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    if (vm.Name?.IndexOf(Search, StringComparison.InvariantCultureIgnoreCase) < 0)
                    {
                        return false;
                    }
                }
                if (!ShowAll)
                {
                    return !vm.IsHidden;
                }
            }
            return true;
        };

        _view.GroupDescriptions.Clear();

        if (GroupItems)
            _view.GroupDescriptions.Add(new PropertyGroupDescription("CategoryName"));
    }

    protected override async Task ReloadItemsAsync()
    {
        TotalBudget = 0;
        await base.ReloadItemsAsync();
        int percentage = 0;
        int total = 0;
        foreach (var category in Items)
        {
            if (category.BudgetedPercentage > 0)
            {
                percentage += category.BudgetedPercentage;
            }
            else
            {
                total += category.BudgetedAmount;
            }
        }
        //TODO: percentage > 100
        //Let x be to total budget
        //Let t = total budgeted
        //Let p = total percentage
        //x - (x * p)/100 = t
        //100x - (x * p) = 100t
        //x(100 - p) = 100t
        //x = 100t/(100 - p)
        TotalBudget = total * 100 / (100 - percentage);
    }
}