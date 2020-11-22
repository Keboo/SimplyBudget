using AutoDI;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudget.ViewModels.MainWindow;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class MainWindowViewModel : ObservableObject,
        IRecipient<DoneAddingItemMessage>,
        IRecipient<CurrentMonthChanged>
    {
        public BudgetViewModel Budget { get; }

        public HistoryViewModel History { get; }

        public AccountsViewModel Accounts { get; }

        private AddItemViewModel? _addItem;
        public AddItemViewModel? AddItem
        {
            get => _addItem;
            set => SetProperty(ref _addItem, value);
        }

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

        public List<DateTime> PastMonths { get; } = new List<DateTime>
        {
            DateTime.Today.AddMonths(0).StartOfMonth(),
            DateTime.Today.AddMonths(-1).StartOfMonth(),
            DateTime.Today.AddMonths(-2).StartOfMonth(),
            DateTime.Today.AddMonths(-3).StartOfMonth(),
            DateTime.Today.AddMonths(-4).StartOfMonth(),
            DateTime.Today.AddMonths(-5).StartOfMonth(),
            DateTime.Today.AddMonths(-6).StartOfMonth()
        };


        public ICommand ShowAddCommand { get; }
        public IMessenger Messenger { get; }
        public ICurrentMonth CurrentMonth { get; }
        private BudgetContext Context { get; }

        public MainWindowViewModel(
            [Dependency]IMessenger? messenger = null,
            [Dependency]ICurrentMonth? currentMonth = null,
            [Dependency]BudgetContext? context = null)
        {
            ShowAddCommand = new RelayCommand<AddType?>(OnShowAdd);

            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Budget = new BudgetViewModel(messenger, currentMonth);
            History = new HistoryViewModel(context, messenger, currentMonth);
            Accounts = new AccountsViewModel(context, messenger);

            Budget.LoadItemsAsync();
            History.LoadItemsAsync();
            Accounts.LoadItemsAsync();

            SelectedMonth = PastMonths.First();

            messenger.Register<DoneAddingItemMessage>(this);
            messenger.Register<CurrentMonthChanged>(this);
        }

        private void OnShowAdd(AddType? addType)
        {
            AddItem = new AddItemViewModel(Context, CurrentMonth, Messenger);
            if (addType != null && addType != AddType.None)
            {
                AddItem.SelectedType = addType.Value;
            }
        }

        public void Receive(DoneAddingItemMessage message) 
            => AddItem = null;

        public void Receive(CurrentMonthChanged message) 
            => SelectedMonth = message.StartOfMonth;
    }
}
