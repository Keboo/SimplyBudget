using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class ExpenseCategoryDetailsViewModel : CollectionViewModelBase<ExpenseCategoryItemViewModel>, 
        IEventListener<TransactionItemEvent>, IEventListener<TransferEvent>
    {
        private readonly int _expenseCategoryID;
        private string _expenseCategoryName;

        private BudgetContext Context { get; } = BudgetContext.Instance;

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
                    _view.SortDescriptions.Add(new SortDescription(nameof(ExpenseCategoryItemViewModel.Date), ListSortDirection.Ascending));
                return _view;
            }
        }

        public string Title => "Details for " + _expenseCategoryName;

        private DateTime _queryStart = DateTime.Now.StartOfMonth();
        public DateTime QueryStart
        {
            get => _queryStart;
            set
            {
                if (SetProperty(ref _queryStart, value))
                    LoadItemsAsync();
            }
        }

        private DateTime _queryEnd = DateTime.Now.EndOfMonth();
        public DateTime QueryEnd
        {
            get => _queryEnd;
            set
            {
                if (SetProperty(ref _queryEnd, value))
                    LoadItemsAsync();
            }
        }

        protected override async Task<IEnumerable<ExpenseCategoryItemViewModel>> GetItems()
        {
            var expenseCategory = await Context.ExpenseCategories.FindAsync(_expenseCategoryID);
            if (expenseCategory is null) return null;
            _expenseCategoryName = expenseCategory.Name;
            OnPropertyChanged(nameof(Title));

            var transactions = await Context.GetTransactionItems(expenseCategory, QueryStart, QueryEnd);

            var rv = new List<ExpenseCategoryItemViewModel>();
            // ReSharper disable LoopCanBeConvertedToQuery
            if (transactions != null)
                foreach (var transaction in transactions)
                    rv.Add(await ExpenseCategoryItemViewModel.Create(Context, transaction));
            // ReSharper restore LoopCanBeConvertedToQuery

            var transfers = await Context.GetTransfers(expenseCategory, QueryStart, QueryEnd);
            // ReSharper disable LoopCanBeConvertedToQuery
            if (transfers != null)
                foreach(var transfer in transfers)
                    rv.Add(await ExpenseCategoryItemViewModel.Create(Context, transfer, expenseCategory.ID));
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
        public static async Task<ExpenseCategoryItemViewModel> Create(BudgetContext context, TransactionItem transactionItem)
        {
            if (transactionItem is null) throw new ArgumentNullException(nameof(transactionItem));
            var transaction = await context.Transactions.FindAsync(transactionItem.TransactionID);
            if (transaction is null) return null;
            return new ExpenseCategoryItemViewModel(transactionItem)
                       {
                           Amount = transactionItem.Amount,
                           Date = transaction.Date,
                           Description = transactionItem.Description
                       };
        }

        public static async Task<ExpenseCategoryItemViewModel> Create(BudgetContext context, Transfer transfer, int expenseCategoryID)
        {
            if (transfer is null) throw new ArgumentNullException(nameof(transfer));

            var amount = transfer.Amount;

            var sb = new StringBuilder();
            sb.Append("Transfer");
            if (transfer.FromExpenseCategoryID == expenseCategoryID)
            {
                var toExpenseCategory = await context.ExpenseCategories.FindAsync(transfer.ToExpenseCategoryID);
                sb.AppendFormat(" to {0}", toExpenseCategory != null ? toExpenseCategory.Name : "(unknown)");
            }
            else if (transfer.ToExpenseCategoryID == expenseCategoryID)
            {
                var fromExpenseCategory = await context.ExpenseCategories.FindAsync(transfer.ToExpenseCategoryID);
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

        public static async Task<ExpenseCategoryItemViewModel> Create(IncomeItem incomeItem)
        {
            if (incomeItem is null) throw new ArgumentNullException("incomeItem");
            var income = await DatabaseManager.GetAsync<Income>(incomeItem.IncomeID);

            return new ExpenseCategoryItemViewModel(incomeItem)
                       {
                           Amount = -1*incomeItem.Amount,
                           Date = income.Date,
                           Description = string.Format("Income: {0}", incomeItem.Description ?? "")
                       };
        }

        private ExpenseCategoryItemViewModel(TransactionItem transactionItem)
        {
            TransactionItemID = transactionItem.ID;
        }

        private ExpenseCategoryItemViewModel(Transfer transfer)
        {
            TransferID = transfer.ID;
        }

        private ExpenseCategoryItemViewModel(IncomeItem incomeItem)
        {
            IncomeItemID = incomeItem.ID;
        }

        public int TransactionItemID { get; }

        public int TransferID { get; }

        public int IncomeItemID { get; }

        private int _amount;
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
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