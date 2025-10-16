using System.Text.RegularExpressions;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using MaterialDesignThemes.Wpf;

using SimplyBudget.Messaging;

using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels;

public partial class MainWindowViewModel : ObservableObject,
    IRecipient<DoneAddingItemMessage>,
    IRecipient<CurrentMonthChanged>,
    IRecipient<AddItemMessage>
{
    public BudgetViewModel Budget { get; }

    public HistoryViewModel History { get; }

    public AccountsViewModel Accounts { get; }

    public ImportViewModel Import { get; }

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private AddItemViewModel? _addItem;

    private DateTime _selectedMonth;
    public DateTime SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, value))
            {
                CurrentMonth.CurrenMonth = value;
            }
        }
    }

    public List<DateTime> PastMonths { get; } =
    [
        DateTime.Today.AddMonths(0).StartOfMonth(),
        DateTime.Today.AddMonths(-1).StartOfMonth(),
        DateTime.Today.AddMonths(-2).StartOfMonth(),
        DateTime.Today.AddMonths(-3).StartOfMonth(),
        DateTime.Today.AddMonths(-4).StartOfMonth(),
        DateTime.Today.AddMonths(-5).StartOfMonth(),
        DateTime.Today.AddMonths(-6).StartOfMonth(),
        DateTime.Today.AddMonths(-7).StartOfMonth(),
        DateTime.Today.AddMonths(-8).StartOfMonth(),
        DateTime.Today.AddMonths(-9).StartOfMonth(),
        DateTime.Today.AddMonths(-10).StartOfMonth(),
        DateTime.Today.AddMonths(-11).StartOfMonth()
    ];

    public IMessenger Messenger { get; }
    public ICurrentMonth CurrentMonth { get; }
    private Func<BudgetContext> ContextFactory { get; }
    private IDataClient DataClient { get; }
    private IDispatcher Dispatcher { get; }

    public MainWindowViewModel(
        IMessenger messenger,
        ICurrentMonth currentMonth,
        IDataClient dataClient,
        Func<BudgetContext> contextFactory,
        IDispatcher dispatcher,
        ISnackbarMessageQueue messageQueue)
    {
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
        ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        DataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
        Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        ArgumentNullException.ThrowIfNull(messageQueue);

        Budget = new(contextFactory, messenger, currentMonth);
        History = new(contextFactory, messenger, currentMonth);
        Accounts = new(dataClient, messenger);
        Import = new(contextFactory, messenger);
        Settings = new(contextFactory, messageQueue, messenger);

        _ = Budget.LoadItemsAsync();
        _ = History.LoadItemsAsync();
        _ = Accounts.LoadItemsAsync();
        _ = Settings.LoadItemsAsync();

        SelectedMonth = PastMonths.First();

        messenger.Register<DoneAddingItemMessage>(this);
        messenger.Register<CurrentMonthChanged>(this);
        messenger.Register<AddItemMessage>(this);
    }

    [RelayCommand]
    private void OnShowAdd(AddType? addType)
    {
        AddItem = new AddItemViewModel(DataClient, CurrentMonth, Messenger, Dispatcher);
        if (addType != null && addType != AddType.None)
        {
            AddItem.SelectedType = addType.Value;
        }
    }

    public void Receive(DoneAddingItemMessage _)
        => AddItem = null;

    public void Receive(CurrentMonthChanged message)
        => SelectedMonth = message.StartOfMonth;

    public void Receive(AddItemMessage message)
    {
        AddItem = new AddItemViewModel(DataClient, CurrentMonth, Messenger, Dispatcher)
        {
            SelectedType = message.Type,
            Date = message.Date,
            Description = message.Description
        };
        switch (message.Type)
        {
            case AddType.Transaction:
                {
                    using var context = ContextFactory();
                    var rules = context.ExpenseCategoryRules
                        .Where(x => x.RuleRegex != null && x.ExpenseCategoryID != null)
                        .ToList();
                    var matchingRule = rules.Where(r => Regex.IsMatch(message.Description ?? "", r.RuleRegex ?? "", RegexOptions.IgnoreCase)).LastOrDefault();
                    AddItem.LineItems.Clear();
                    AddItem.LineItems.AddRange(message.Items
                        .Select(x => new LineItemViewModel(AddItem.ExpenseCategories, Messenger)
                        {
                            Amount = x.Amount,
                            SelectedCategory = AddItem.ExpenseCategories.FirstOrDefault(x => x.ID == matchingRule?.ExpenseCategoryID)
                        }));
                    break;
                }
            case AddType.Income:
                AddItem.TotalAmount = message.Items.Sum(x => x.Amount);
                break;
        }
        AddItem.Receive(new LineItemAmountUpdated());
    }
}
