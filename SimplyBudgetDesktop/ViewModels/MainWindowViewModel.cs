using AutoDI;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.ViewModels.MainWindow;
using SimplyBudgetShared.Data;
using System;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class MainWindowViewModel : ObservableObject,
        IRecipient<DoneAddingItemMessage>
    {
        public BudgetViewModel Budget { get; }

        public HistoryViewModel History { get; }

        public AccountsViewModel Accounts { get; }

        private AddItemViewModel _addItem;
        public AddItemViewModel AddItem
        {
            get => _addItem;
            set => SetProperty(ref _addItem, value);
        }

        public ICommand ShowAddCommand { get; }
        public IMessenger Messenger { get; }
        private BudgetContext Context { get; }

        public MainWindowViewModel(
            [Dependency]IMessenger messenger = null, 
            [Dependency]BudgetContext context = null)
        {
            ShowAddCommand = new RelayCommand(OnShowAdd);

            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Budget = new BudgetViewModel(messenger);
            History = new HistoryViewModel(context, messenger);
            Accounts = new AccountsViewModel(context, messenger);

            Budget.LoadItemsAsync();
            History.LoadItemsAsync();
            Accounts.LoadItemsAsync();

            messenger.Register(this);
        }

        private void OnShowAdd()
        {
            AddItem = new AddItemViewModel(Context, Messenger);
        }

        public void Receive(DoneAddingItemMessage message)
        {
            AddItem = null;
        }
    }
}
