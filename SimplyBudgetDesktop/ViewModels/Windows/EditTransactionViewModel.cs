using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.Toolkit.Mvvm.Input;
using SimplyBudget.Collections;
using SimplyBudgetShared.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class EditTransactionViewModel : ViewEditViewModel<Transaction>, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly RelayCommand _addTransactionCommand;
        private readonly RelayCommand<TransactionItemDetailsViewModel> _removeTransactionCommand; 

        private Transaction _existingTransaction;

        public EditTransactionViewModel()
        {
            Transactions = new ObservableCollection<TransactionItemDetailsViewModel>
                                {
                                    new TransactionItemDetailsViewModel(UpdateRemaining)
                                };

            _addTransactionCommand = new RelayCommand(OnAddTransaction);
            _removeTransactionCommand = new RelayCommand<TransactionItemDetailsViewModel>(OnRemoveTransaction,
                                                                                             CanRemoveTransaction);

            _date = DateTime.Today;
        }

        public ObservableCollection<TransactionItemDetailsViewModel> Transactions { get; }

        public ICommand AddTransactionCommand => _addTransactionCommand;

        public ICommand RemoveTransactionCommand => _removeTransactionCommand;

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private string _errorRemainingAmount;
        public string ErrorRemainingAmount
        {
            get => _errorRemainingAmount;
            set => SetProperty(ref _errorRemainingAmount, value);
        }

        private int _total;
        public int Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }

        private int _remainingAmount;
        public int RemainingAmount
        {
            get => _remainingAmount;
            set => SetProperty(ref _remainingAmount, value);
        }

        protected override async Task CreateAsync()
        {
            bool hasError = false;
            foreach (var transactionVM in Transactions.Where(transactionVM => transactionVM.IsEmpty() == false))
            {
                if (transactionVM.Amount == 0)
                {
                    transactionVM.ErrorMessage = "An amount is required";
                    hasError = true;
                }
                if (transactionVM.ExpenseCategoryID <= 0)
                {
                    transactionVM.ErrorMessage = "An expense category must be specified";
                    hasError = true;
                }
            }
            if (hasError)
                return;

            var transactions = Transactions.Where(x => x.IsEmpty() == false).ToArray();
            if (transactions.Length == 0) //This case should be prevented by the UI
            {
                ErrorMessage = "At least one expense must be specified";
                return;
            }
            if (transactions.Length > 1 && RemainingAmount != 0)
            {
                ErrorRemainingAmount = "The entire amount must be allocated";
                return;
            }

            var transaction = new Transaction { Date = Date, Description = Description };

            await transaction.Save();

            foreach (var transactionItem in transactions)
            {
                var item = new TransactionItem
                               {
                                   Amount = transactionItem.Amount,
                                   ExpenseCategoryID = transactionItem.ExpenseCategoryID,
                                   Description = Description,
                                   TransactionID = transaction.ID
                               };
                await item.Save();
            }
            RequestClose.Raise(this, EventArgs.Empty);
        }

        protected override async Task SaveAsync()
        {
            if (_existingTransaction is null) return;

            var transactionItemVMs = Transactions.Where(x => x.IsEmpty() == false).ToArray();
            if (transactionItemVMs.Length == 0) //This case should be blocked by the UI
            {
                ErrorMessage = "At least one expense must be specified";
                return;
            }
            
            if (transactionItemVMs.Length > 1 && RemainingAmount == 0)
            {
                ErrorRemainingAmount = "The entire amount must be allocated";
                return;
            }

            _existingTransaction.Date = Date;
            _existingTransaction.Description = Description;
            
            var existingTransactionItems = await _existingTransaction.GetTransactionItems();
            var existingDic = existingTransactionItems.ToDictionary(x => x.ID);

            foreach (var transactionItemVM in transactionItemVMs)
            {
                TransactionItem existingTransaction;
                if (transactionItemVM.ExistingTransactionItemID > 0 &&
                    existingDic.TryGetValue(transactionItemVM.ExistingTransactionItemID,
                                                     out existingTransaction))
                {
                    existingDic.Remove(transactionItemVM.ExistingTransactionItemID);
                    //Update existing item
                    existingTransaction = existingTransaction ?? new TransactionItem();
                    existingTransaction.Amount = transactionItemVM.Amount;
                    existingTransaction.ExpenseCategoryID = transactionItemVM.ExpenseCategoryID;
                    existingTransaction.Description = transactionItemVM.Description;
                    await existingTransaction.Save();
                }
                else
                {
                    //Add new item
                    await _existingTransaction.AddTransactionItem(transactionItemVM.ExpenseCategoryID, transactionItemVM.Amount,
                                                                transactionItemVM.Description);
                }
            }

            //Remove any remaining transactions
            if (existingDic.Count > 0)
            {
                foreach (var transactionItem in existingDic.Values)
                {
                    await transactionItem.Delete();
                }
            }

            await _existingTransaction.Save();
            RequestClose.Raise(this, EventArgs.Empty);
        }

        protected override async Task SetPropertiesToEditAsync(Transaction transaction)
        {
            Date = transaction.Date;
            Description = transaction.Description;

            Transactions.Clear();
            var transactionItems = await transaction.GetTransactionItems();
            if (transactionItems != null)
            {
                foreach (var transactionItem in transactionItems)
                    Transactions.Add(new TransactionItemDetailsViewModel(transactionItem, UpdateRemaining));
                Total = Transactions.Sum(x => x.Amount);
            }

            UpdateRemaining();

            _existingTransaction = transaction;
        }

        private void OnAddTransaction()
        {
            if (Transactions.Count == 1 && Total == 0)
                Total = Transactions[0].Amount;
            
            Transactions.Add(new TransactionItemDetailsViewModel(UpdateRemaining));

            UpdateRemaining();
        }

        private void OnRemoveTransaction(TransactionItemDetailsViewModel viewModel)
        {
            Transactions.Remove(viewModel);
            if (Transactions.Count == 1 && Transactions[0].Amount == 0)
                Transactions[0].Amount = Total;
            
            UpdateRemaining();
        }

        private bool CanRemoveTransaction(TransactionItemDetailsViewModel viewModel)
        {
            return viewModel != null;
        }

        private void UpdateRemaining()
        {
            ErrorRemainingAmount = "";
            ErrorMessage = "";
            RemainingAmount = Total - Transactions.Sum(x => x.Amount);
        }
    }

    internal class TransactionItemDetailsViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly Action _itemUpdated;

        public TransactionItemDetailsViewModel(Action itemUpdated)
        {
            if (itemUpdated is null) throw new ArgumentNullException("itemUpdated");
            _itemUpdated = itemUpdated;

            ExpenseCategories = CollectionViewSource.GetDefaultView(ExpenseCategoryCollection.Instance);
            ExpenseCategories.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public TransactionItemDetailsViewModel(TransactionItem transactionItem,
            Action amountUpdated)
            : this(amountUpdated)
        {
            if (transactionItem is null) throw new ArgumentNullException("transactionItem");
            ExistingTransactionItemID = transactionItem.ID;
            ExpenseCategoryID = transactionItem.ExpenseCategoryID;
            Amount = transactionItem.Amount;
            Description = transactionItem.Description;
        }

        public int ExistingTransactionItemID { get; }

        public ICollectionView ExpenseCategories { get; }

        private int _amount;
        public int Amount
        {
            get => _amount;
            set
            {
                if (SetProperty(ref _amount, value))
                {
                    _itemUpdated();
                    ErrorMessage = "";
                }
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private int _expenseCategoryID;
        public int ExpenseCategoryID
        {
            get => _expenseCategoryID;
            set
            {
                if (SetProperty(ref _expenseCategoryID, value))
                {
                    ErrorMessage = "";
                    _itemUpdated();
                }
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsEmpty()
        {
            return Amount == 0 && ExpenseCategoryID <= 0;
        }
    }
}