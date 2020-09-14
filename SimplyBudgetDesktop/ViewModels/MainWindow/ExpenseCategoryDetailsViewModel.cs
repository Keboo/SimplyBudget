using System.Text;
using JetBrains.Annotations;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class ExpenseCategoryDetailsViewModel : CollectionViewModelBase<ExpenseCategoryItemViewModel>, 
        IEventListener<TransactionItemEvent>, IEventListener<TransferEvent>
    {
        private readonly int _expenseCategoryID;
        private string _expenseCategoryName;

        public ExpenseCategoryDetailsViewModel(int expenseCategoryID)
        {
            _expenseCategoryID = expenseCategoryID;
            NotificationCenter.Register<TransferEvent>(this);
            NotificationCenter.Register<TransactionItemEvent>(this);
        }

        public ICollectionView ExpenseCategoryTransactionsView
        {
            get
            {
                if (_view.SortDescriptions.Count == 0)
                    _view.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
                return _view;
            }
        }

        public string Title
        {
            get { return "Details for " + _expenseCategoryName; }
        }

        private DateTime _queryStart = DateTime.Now.StartOfMonth();
        public DateTime QueryStart
        {
            get { return _queryStart; }
            set
            {
                if (SetProperty(ref _queryStart, value))
                    LoadItemsAsync();
            }
        }

        private DateTime _queryEnd = DateTime.Now.EndOfMonth();
        public DateTime QueryEnd
        {
            get { return _queryEnd; }
            set
            {
                if (SetProperty(ref _queryEnd, value))
                    LoadItemsAsync();
            }
        }

        protected override async Task<IEnumerable<ExpenseCategoryItemViewModel>> GetItems()
        {
            var expenseCategory = await DatabaseManager.GetAsync<ExpenseCategory>(_expenseCategoryID);
            if (expenseCategory == null) return null;
            _expenseCategoryName = expenseCategory.Name;
            RaisePropertyChanged(() => Title);

            var transactions = await expenseCategory.GetTransactionItems(QueryStart, QueryEnd);

            var rv = new List<ExpenseCategoryItemViewModel>();
            // ReSharper disable LoopCanBeConvertedToQuery
            if (transactions != null)
                foreach (var transaction in transactions)
                    rv.Add(await ExpenseCategoryItemViewModel.Create(transaction));
            // ReSharper restore LoopCanBeConvertedToQuery

            var transfers = await expenseCategory.GetTransfers(QueryStart, QueryEnd);
            // ReSharper disable LoopCanBeConvertedToQuery
            if (transfers != null)
                foreach(var transfer in transfers)
                    rv.Add(await ExpenseCategoryItemViewModel.Create(transfer, expenseCategory.ID));
            // ReSharper restore LoopCanBeConvertedToQuery

            var incomeItems = await expenseCategory.GetIncomeItems(QueryStart, QueryEnd);
            // ReSharper disable LoopCanBeConvertedToQuery
            if (incomeItems != null)
                foreach(var incomeItem in incomeItems)
                    rv.Add(await ExpenseCategoryItemViewModel.Create(incomeItem));
            // ReSharper restore LoopCanBeConvertedToQuery

            return rv;
        }

        public async void HandleEvent(TransactionItemEvent @event)
        {
            await ReloadItemsAsync();
        }

        public async void HandleEvent(TransferEvent @event)
        {
            await ReloadItemsAsync();
        }
    }

    internal class ExpenseCategoryItemViewModel : ViewModelBase, IDatabaseItem
    {
        public static async Task<ExpenseCategoryItemViewModel> Create([NotNull] TransactionItem transactionItem)
        {
            if (transactionItem == null) throw new ArgumentNullException("transactionItem");
            var transaction = await DatabaseManager.Instance.Connection.GetAsync<Transaction>(transactionItem.TransactionID);
            if (transaction == null) return null;
            return new ExpenseCategoryItemViewModel(transactionItem)
                       {
                           Amount = transactionItem.Amount,
                           Date = transaction.Date,
                           Description = transactionItem.Description
                       };
        }

        public static async Task<ExpenseCategoryItemViewModel> Create([NotNull] Transfer transfer, int expenseCategoryID)
        {
            if (transfer == null) throw new ArgumentNullException("transfer");

            var amount = transfer.Amount;

            var sb = new StringBuilder();
            sb.Append("Transfer");
            if (transfer.FromExpenseCategoryID == expenseCategoryID)
            {
                var toExpenseCategory = await DatabaseManager.Instance.Connection.GetAsync<ExpenseCategory>(transfer.ToExpenseCategoryID);
                sb.AppendFormat(" to {0}", toExpenseCategory != null ? toExpenseCategory.Name : "(unknown)");
            }
            else if (transfer.ToExpenseCategoryID == expenseCategoryID)
            {
                var fromExpenseCategory = await DatabaseManager.Instance.Connection.GetAsync<ExpenseCategory>(transfer.ToExpenseCategoryID);
                sb.AppendFormat(" from {0}", fromExpenseCategory != null ? fromExpenseCategory.Name : "(unknown)");
                amount *= -1;
            }
            sb.AppendFormat(": {0}", transfer.Description);

            return new ExpenseCategoryItemViewModel(transfer)
                       {
                           Amount = amount,
                           Date = transfer.Date,
                           Description = sb.ToString()
                       };
        }

        public static async Task<ExpenseCategoryItemViewModel> Create([NotNull] IncomeItem incomeItem)
        {
            if (incomeItem == null) throw new ArgumentNullException("incomeItem");
            var income = await DatabaseManager.GetAsync<Income>(incomeItem.IncomeID);

            return new ExpenseCategoryItemViewModel(incomeItem)
                       {
                           Amount = -1*incomeItem.Amount,
                           Date = income.Date,
                           Description = string.Format("Income: {0}", incomeItem.Description ?? "")
                       };
        }

        private readonly int _transactionItemID;
        private readonly int _transferID;
        private readonly int _incomeItemID;

        private ExpenseCategoryItemViewModel(TransactionItem transactionItem)
        {
            _transactionItemID = transactionItem.ID;
        }

        private ExpenseCategoryItemViewModel(Transfer transfer)
        {
            _transferID = transfer.ID;
        }

        private ExpenseCategoryItemViewModel(IncomeItem incomeItem)
        {
            _incomeItemID = incomeItem.ID;
        }

        public int TransactionItemID
        {
            get { return _transactionItemID; }
        }

        public int TransferID
        {
            get { return _transferID; }
        }

        public int IncomeItemID
        {
            get { return _incomeItemID; }
        }

        private int _amount;
        public int Amount
        {
            get { return _amount; }
            set { SetProperty(ref _amount, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set { SetProperty(ref _date, value); }
        }



        public async Task<BaseItem> GetItem()
        {
            if (TransferID > 0)
                return await DatabaseManager.GetAsync<Transfer>(TransferID);
            if (TransactionItemID > 0)
                return await DatabaseManager.GetAsync<TransactionItem>(TransactionItemID);
            if (IncomeItemID > 0)
                return await DatabaseManager.GetAsync<IncomeItem>(IncomeItemID);
            return null;
        }
    }
}