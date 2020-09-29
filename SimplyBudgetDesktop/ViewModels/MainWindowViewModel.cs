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
        IRecipient<AddItemViewModel.ItemAddedMessage>
    {
        public BudgetViewModel Budget { get; }

        public HistoryViewModel History { get; }

        private AddItemViewModel _addItem;
        public AddItemViewModel AddItem
        {
            get => _addItem;
            set => SetProperty(ref _addItem, value);
        }

        public ICommand ShowAddCommand { get; }
        public IMessenger Messenger { get; }

        public MainWindowViewModel(
            [Dependency]IMessenger messenger = null, 
            [Dependency]BudgetContext context = null)
        {
            ShowAddCommand = new RelayCommand(OnShowAdd);

            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            Budget = new BudgetViewModel(messenger);
            History = new HistoryViewModel(context);

            Budget.LoadItemsAsync();
            History.LoadItemsAsync();

            messenger.Register(this);
        }

        private void OnShowAdd()
        {
            AddItem = new AddItemViewModel(Messenger);
        }

        public void Receive(AddItemViewModel.ItemAddedMessage message)
        {
            AddItem = null;
        }
    }
}
