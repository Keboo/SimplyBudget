using System.Collections.ObjectModel;
using System.Windows.Data;

using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SimplyBudget.Messaging;
using SimplyBudget.Validation;

using SimplyBudgetShared.Data;
using SimplyBudgetShared.Threading;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels;

public partial class AddItemViewModel : ValidationViewModel,
    IRecipient<LineItemAmountUpdated>,
    IRecipient<CurrentMonthChanged>
{
    public IDataClient DataClient { get; }
    public ICurrentMonth CurrentMonth { get; }
    public IMessenger Messenger { get; }

    public ObservableCollection<LineItemViewModel> LineItems { get; } = [];

    public IList<AddType> AddTypes { get; } =
    [
        AddType.Transaction,
        AddType.Income,
        AddType.Transfer
    ];

    [ObservableProperty]
    private AddType _selectedType;
    partial void OnSelectedTypeChanged(AddType value)
    {
        LineItems.Clear();
        TotalAmount = 0;
        UpdateRemaining();

        switch (value)
        {
            case AddType.Transaction:
                LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger));
                break;
            case AddType.Income:
                LineItems.AddRange(ExpenseCategories
                    .Select(x => new LineItemViewModel(ExpenseCategories, Messenger)
                    {
                        SelectedCategory = x
                    })
                    .OrderByDescending(x => x.SelectedCategory?.UsePercentage)
                    .ThenBy(x => x.SelectedCategory?.Name));
                LoadDesiredAmounts();
                break;
            case AddType.Transfer:
                LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger));
                LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger) { DesiredAmount = -1 });
                Date = DateTime.Today;
                break;
        }
    }

    [ObservableProperty]
    private int _totalAmount;

    partial void OnTotalAmountChanged(int value)
    {
        if (SelectedType == AddType.Income)
        {
            foreach (var lineItem in LineItems.Where(x => x.SelectedCategory?.UsePercentage == true))
            {
                lineItem.DesiredAmount = (int)(value * lineItem.SelectedCategory!.BudgetedPercentage / 100.0);
            }
        }
        UpdateRemaining();
    }

    [ObservableProperty]
    private int _remainingAmount;

    [ObservableProperty]
    private string? _description;

    [property: ReasonableDate]
    [ObservableProperty]
    private DateTime? _date;

    public ObservableCollection<ExpenseCategory> ExpenseCategories { get; } = [];

    public AddItemViewModel(IDataClient dataClient,
        ICurrentMonth currentMonth,
        IMessenger messenger,
        IDispatcher dispatcher)
    {
        DataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
        CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

        Messenger.Register<LineItemAmountUpdated>(this);
        Messenger.Register<CurrentMonthChanged>(this);

        ExpenseCategories = [];

        SelectedType = AddType.Transaction;

        PropertyChanged += (_, _) => ClearValidationErrors(nameof(SubmitCommand));
        dispatcher.InvokeAsync(() => BindingOperations.EnableCollectionSynchronization(LineItems, new object()));
    }

    public async Task LoadAsync()
    {
        var expenseCategories = await DataClient.ExpenseCategories
            .Query()
            .Where(x => x.IsHidden == false)
            .OrderBy(x => x.Name)
            .ToListAsync();
        ExpenseCategories.Clear();
        ExpenseCategories.AddRange(expenseCategories);
    }

    [RelayCommand]
    private void OnCancel()
        => Messenger.Send(new DoneAddingItemMessage());

    [RelayCommand]
    private void OnRemoveItem(LineItemViewModel? item)
    {
        if (SelectedType == AddType.Transaction &&
            LineItems.Count > 1 &&
            item is not null)
        {
            LineItems.Remove(item);
            if (LineItems.Count == 1)
            {
                LineItems[0].Amount = TotalAmount;
            }
            UpdateRemaining();
        }
    }

    [RelayCommand]
    private void OnAddItem()
    {
        if (SelectedType == AddType.Transaction &&
            LineItems.Count == 1)
        {
            TotalAmount = LineItems[0].Amount;
            LineItems[0].Amount = 0;
        }
        LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger));
    }

    [RelayCommand]
    private void OnAutoAllocate()
    {
        int total = RemainingAmount;
        foreach (var lineItem in LineItems.OrderByDescending(x => x.SelectedCategory?.UsePercentage))
        {
            int desiredAmount = lineItem.DesiredAmount;
            if (lineItem.SelectedCategory?.UsePercentage == true)
            {
                desiredAmount = (int)(TotalAmount * (lineItem.SelectedCategory.BudgetedPercentage / 100.0));
            }
            lineItem.Amount = Math.Min(total, desiredAmount);
            total -= lineItem.Amount;
        }
    }

    private async void LoadDesiredAmounts()
    {
        foreach (var lineItem in LineItems.Where(x => x.SelectedCategory is not null))
        {
            var desiredAmount = await DataClient.GetRemainingBudgetAmount(lineItem.SelectedCategory!, CurrentMonth.CurrenMonth);
            lineItem.DesiredAmount = desiredAmount;
            lineItem.SetAmountCallback = x =>
            {
                if (x.SelectedCategory?.UsePercentage == true)
                {
                    return (int)Math.Round(x.SelectedCategory.BudgetedPercentage / 100.0 * TotalAmount);
                }
                return Math.Min(RemainingAmount, desiredAmount);
            };
        }
    }

    [RelayCommand]
    private async Task OnSubmit(bool? ignoreBudget)
    {
        var errors = await (SelectedType switch
        {
            AddType.Transaction => TrySubmitTransaction(ignoreBudget == true),
            AddType.Income => TrySubmitIncome(ignoreBudget == true),
            AddType.Transfer => TrySubmitTransfer(ignoreBudget == true),
            _ => throw new InvalidOperationException()
        }).ToListAsync();

        SetValidationErrors(nameof(SubmitCommand), errors);

        if (!errors.Any())
        {
            Messenger.Send(new DoneAddingItemMessage());
        }
    }

    private void UpdateRemaining() => RemainingAmount = TotalAmount - LineItems.Sum(x => x.Amount);

    private async IAsyncEnumerable<string> TrySubmitTransfer(bool ignoreBudget)
    {
        if (Date is null)
        {
            yield return "Date is required";
            yield break;
        }
        if (TotalAmount <= 0)
        {
            yield return "Total amount is required";
            yield break;
        }

        var items = LineItems.Where(x => x.SelectedCategory != null).ToList();
        if (items.Count != 2)
        {
            yield return "Both a From and To categories must be defined";
            yield break;
        }

        if (items[0].SelectedCategory?.ID == items[1].SelectedCategory?.ID)
        {
            yield return "From and To categories must be different";
            yield break;
        }

        var rv = await DataClient.AddTransferAsync(Description ?? "", Date.Value, ignoreBudget, TotalAmount,
            items[0].SelectedCategory!, items[1].SelectedCategory!, CancellationToken.None);
        if (rv is null)
        {
            yield return "Failed to add transfer";
        }
    }

    private async IAsyncEnumerable<string> TrySubmitIncome(bool ignoreBudget)
    {
        if (Date is null)
        {
            yield return "Date is required";
            yield break;
        }
        if (TotalAmount <= 0)
        {
            yield return "Total amount is required";
            yield break;
        }

        var items = GetValidLineItems().ToList();
        if (items.Sum(x => x.Amount) != TotalAmount)
        {
            yield return "All income must be allocated to a category";
            yield break;
        }

        if (items.FirstOrDefault(x => x.Amount < 0) is { } negativeItem)
        {
            yield return $"{negativeItem.SelectedCategory?.Name ?? "<empty>"} has a negative amount";
            yield break;
        }

        var rv = await DataClient.AddIncomeAsync(Description ?? "", Date.Value, ignoreBudget,
            [.. items.Select(x => (x.Amount, x.SelectedCategory!.ID))], CancellationToken.None);
        if (rv is null)
        {
            yield return "Failed to add income";
        }
    }

    private async IAsyncEnumerable<string> TrySubmitTransaction(bool ignoreBudget)
    {
        if (Date is null)
        {
            yield return "Date is required";
            yield break;
        }

        var items = GetValidLineItems().ToList();
        if (items.Count == 0)
        {
            yield return "At least one line item must be completed";
            yield break;
        }

        var rv = await DataClient.AddTransactionAsync(Description ?? "", Date.Value, ignoreBudget,
            [.. items.Select(vm => (vm.Amount, vm.SelectedCategory!.ID))], CancellationToken.None);
        if (rv is null)
        {
            yield return "Failed to add transaction";
        }
    }

    private IEnumerable<LineItemViewModel> GetValidLineItems()
        => LineItems.Where(x => x.SelectedCategory != null && x.Amount > 0);

    public void Receive(LineItemAmountUpdated message)
    {
        switch (SelectedType)
        {
            case AddType.Transaction:
            case AddType.Income:
                UpdateRemaining();
                break;
        }
    }

    public async void Receive(CurrentMonthChanged message)
    {
        await TaskEx.Run(() =>
        {
            _ = ValidateModel();
            if (SelectedType == AddType.Income)
            {
                SelectedType = AddType.None;
                SelectedType = AddType.Income;
            }
        });
    }
}
