using SimplyBudget.Events;
using Microsoft.Practices.Prism.Commands;
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

        private readonly DelegateCommand<ExpenseCategoryViewModel> _showExpenseCategoryDetailsCommand;
        private readonly DelegateCommand<ITransactionItem> _showTransactionDetailsCommand;
        private readonly DelegateCommand<AccountViewModel> _showAccountDetailsCommand; 
        private readonly DelegateCommand _backCommand;
        private readonly DelegateCommand _showTransactionItemsCommand;
        private readonly DelegateCommand _showIncomeItemsCommand;
        private readonly DelegateCommand _showAccountInformationCommand;

        public MainWindowViewModel()
        {
            _viewStack = new Stack<ViewModelBase>();

            _showExpenseCategoryDetailsCommand = new DelegateCommand<ExpenseCategoryViewModel>(OnShowExpenseCategoryDetails);
            _showTransactionDetailsCommand = new DelegateCommand<ITransactionItem>(OnShowTransactionDetails);
            _showAccountDetailsCommand = new DelegateCommand<AccountViewModel>(OnShowAccountDetails);
            _backCommand = new DelegateCommand(OnBack, CanBack);
            _showTransactionItemsCommand = new DelegateCommand(OnShowTransactionItems);
            _showIncomeItemsCommand = new DelegateCommand(OnShowIncomeItems);
            _showAccountInformationCommand = new DelegateCommand(OnShowAccountInformation);

            NotificationCenter.Register<SnapshotLoadedEvent>(this);
            NotificationCenter.Register<SnapshotCreatedEvent>(this);

            if (DesignerHelper.IsDesignMode == false)
                LoadBudget();
        }

        public ICommand ShowExpenseCategoryDetailsCommand
        {
            get { return _showExpenseCategoryDetailsCommand; }
        }

        public ICommand ShowTransactionsCommand
        {
            get { return _showTransactionItemsCommand; }
        }

        public ICommand ShowAccountDetailsCommand
        {
            get { return _showAccountDetailsCommand; }
        }

        public ICommand ShowIncomeItemsCommand
        {
            get { return _showIncomeItemsCommand; }
        }

        public ICommand ShowAccountInformationCommand
        {
            get { return _showAccountInformationCommand; }
        }

        public DelegateCommand<ITransactionItem> ShowTransactionDetailsCommand
        {
            get { return _showTransactionDetailsCommand; }
        }

        public ViewModelBase TopView
        {
            get
            {
                if (_viewStack.Count > 0)
                    return _viewStack.Peek();
                return null;
            }
        }

        public ICommand BackCommand
        {
            get { return _backCommand; }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
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
            if (viewModel == null) return;
            PushView(new ExpenseCategoryDetailsViewModel(viewModel.ExpenseCategoryID));
        }

        private async void OnShowTransactionDetails(ITransactionItem viewModel)
        {
            if (viewModel == null) return;
            var transactionVM = viewModel as TransactionViewModel;
            if (transactionVM == null) return;

            var transaction = await DatabaseManager.GetAsync<Transaction>(transactionVM.TransactionID);
            
            if (transaction != null)
                PushView(new TransactionDetailsViewModel(transaction));
        }

        private void OnShowAccountDetails(AccountViewModel viewModel)
        {
            if (viewModel == null) return;
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
            _backCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(() => TopView);
        }

        private void OnBack()
        {
            var vm = _viewStack.Pop();
            var dataGridViewModel = vm as CollectionViewModelBase;
            if (dataGridViewModel != null)
                dataGridViewModel.UnloadItems();
            _backCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(() => TopView);
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