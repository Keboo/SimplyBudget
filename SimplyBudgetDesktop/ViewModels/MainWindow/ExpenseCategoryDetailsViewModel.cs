using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class ExpenseCategoryDetailsViewModel : CollectionViewModelBaseOld<ExpenseCategoryItemViewModel> 
    {
        private readonly int _expenseCategoryID;
        private string _expenseCategoryName;

        private BudgetContext Context { get; } = BudgetContext.Instance;

        public ExpenseCategoryDetailsViewModel(int expenseCategoryID)
        {
            _expenseCategoryID = expenseCategoryID;
            //NotificationCenter.Register<TransferEvent>(this);
            //NotificationCenter.Register<TransactionItemEvent>(this);
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

            var incomeItems = await Context.GetIncomeItems(expenseCategory, QueryStart, QueryEnd);
            // ReSharper disable LoopCanBeConvertedToQuery
            if (incomeItems != null)
                foreach(var incomeItem in incomeItems)
                    rv.Add(await ExpenseCategoryItemViewModel.Create(Context, incomeItem));
            // ReSharper restore LoopCanBeConvertedToQuery

            return rv;
        }

        //public async void HandleEvent(TransactionItemEvent @event)
        //{
        //    await ReloadItemsAsync();
        //}

        //public async void HandleEvent(TransferEvent @event)
        //{
        //    await ReloadItemsAsync();
        //}
    }
}