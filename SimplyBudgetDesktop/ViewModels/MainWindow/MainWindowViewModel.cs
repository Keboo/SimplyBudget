using Microsoft.Toolkit.Mvvm.Input;
using SimplyBudget.Events;
using SimplyBudget.Properties;
using SimplyBudget.Utilities;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System.Collections.Generic;
using System.Windows.Input;

#if !DEBUG
using System;
using System.IO;
#endif

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class MainWindowViewModel : ViewModelBase, 
        IEventListener<SnapshotLoadedEvent>,
        IEventListener<SnapshotCreatedEvent>
    {
        private readonly Stack<ViewModelBase> _viewStack;

        private readonly RelayCommand<ExpenseCategoryViewModel> _showExpenseCategoryDetailsCommand;
        private readonly RelayCommand<AccountViewModel> _showAccountDetailsCommand; 
        private readonly RelayCommand _backCommand;
        private readonly RelayCommand _showTransactionItemsCommand;
        private readonly RelayCommand _showIncomeItemsCommand;
        private readonly RelayCommand _showAccountInformationCommand;

        private BudgetContext Context { get; } = BudgetContext.Instance;

        public MainWindowViewModel()
        {
            _viewStack = new Stack<ViewModelBase>();

            _showExpenseCategoryDetailsCommand = new RelayCommand<ExpenseCategoryViewModel>(OnShowExpenseCategoryDetails);
            ShowTransactionDetailsCommand = new RelayCommand<ITransactionItem>(OnShowTransactionDetails);
            _showAccountDetailsCommand = new RelayCommand<AccountViewModel>(OnShowAccountDetails);
            _backCommand = new RelayCommand(OnBack, CanBack);
            _showTransactionItemsCommand = new RelayCommand(OnShowTransactionItems);
            _showIncomeItemsCommand = new RelayCommand(OnShowIncomeItems);
            _showAccountInformationCommand = new RelayCommand(OnShowAccountInformation);

            NotificationCenter.Register<SnapshotLoadedEvent>(this);
            NotificationCenter.Register<SnapshotCreatedEvent>(this);

            if (DesignerHelper.IsDesignMode == false)
                LoadBudget();
        }

        public ICommand ShowExpenseCategoryDetailsCommand => _showExpenseCategoryDetailsCommand;

        public ICommand ShowTransactionsCommand => _showTransactionItemsCommand;

        public ICommand ShowAccountDetailsCommand => _showAccountDetailsCommand;

        public ICommand ShowIncomeItemsCommand => _showIncomeItemsCommand;

        public ICommand ShowAccountInformationCommand => _showAccountInformationCommand;

        public RelayCommand<ITransactionItem> ShowTransactionDetailsCommand { get; }

        public ViewModelBase TopView
        {
            get
            {
                if (_viewStack.Count > 0)
                    return _viewStack.Peek();
                return null;
            }
        }

        public ICommand BackCommand => _backCommand;

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private async void LoadBudget()
        {
            string folderPath = "";
            if (string.IsNullOrWhiteSpace(Settings.Default.DataDirectory))
            {
#if !DEBUG
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimplyBudget");
                if (Directory.Exists(folderPath) == false)
                    Directory.CreateDirectory(folderPath);
#endif
            }
            else
            {
                folderPath = Settings.Default.DataDirectory;
            }

            await DatabaseManager.Instance.InitDatabase(folderPath);

            PushView(new ExpenseCategoriesViewModel());
        }

        private void OnShowExpenseCategoryDetails(ExpenseCategoryViewModel viewModel)
        {
            if (viewModel is null) return;
            PushView(new ExpenseCategoryDetailsViewModel(viewModel.ExpenseCategoryID));
        }

        private async void OnShowTransactionDetails(ITransactionItem viewModel)
        {
            if (viewModel is null) return;
            var transactionVM = viewModel as TransactionViewModel;
            if (transactionVM is null) return;

            var transaction = await Context.Transactions.FindAsync(transactionVM.TransactionID);
            
            if (transaction != null)
                PushView(new TransactionDetailsViewModel(transaction));
        }

        private void OnShowAccountDetails(AccountViewModel viewModel)
        {
            if (viewModel is null) return;
            PushView(new AccountExpenseCategoriesViewModel(viewModel.AccountID));
        }

        private void OnShowTransactionItems()
        {
            PushView(new TransactionsViewModel());
        }

        private void OnShowIncomeItems()
        {
            PushView(new IncomeItemsViewModel());
        }

        private void OnShowAccountInformation()
        {
            PushView(new AccountsViewModel());
        }

        private bool CanBack()
        {
            return _viewStack.Count > 1;
        }

        private void PushView(ViewModelBase viewModel)
        {
            //Don't push the view model if it matches the top view model
            if (_viewStack.Count > 0 && _viewStack.Peek().GetType() == viewModel.GetType())
                return;

            var dataGridViewModel = viewModel as CollectionViewModelBase;
            if (dataGridViewModel != null)
                dataGridViewModel.LoadItemsAsync();
            _viewStack.Push(viewModel);
            _backCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(TopView));
        }

        private void OnBack()
        {
            var vm = _viewStack.Pop();
            var dataGridViewModel = vm as CollectionViewModelBase;
            if (dataGridViewModel != null)
                dataGridViewModel.UnloadItems();
            _backCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(TopView));
        }

        public void HandleEvent(SnapshotLoadedEvent @event)
        {
            _viewStack.Clear();
            LoadBudget();
            StatusMessage = "Loaded snapshot";
        }

        public void HandleEvent(SnapshotCreatedEvent @event)
        {
            StatusMessage = "Snapshot created";
        }
    }
}