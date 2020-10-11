using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class TransactionDetailsViewModel : CollectionViewModelBaseOld<TransactionItemViewModel> 
    {
        private readonly Transaction _transaction;

        private BudgetContext Context { get; } = BudgetContext.Instance;

        public TransactionDetailsViewModel(Transaction transaction)
        {
            if (transaction is null) throw new ArgumentNullException("transaction");
            _transaction = transaction;
            //NotificationCenter.Register(this);
        }

        public ICollectionView TransactionItemsView => _view;

        public string Title => "Transaction Details";

        protected override async Task<IEnumerable<TransactionItemViewModel>> GetItems()
        {
            var rv = new List<TransactionItemViewModel>();
            foreach (var item in await _transaction.GetTransactionItems())
            {
                var expenseCategory = await Context.ExpenseCategories.FindAsync(item.ExpenseCategoryID);
                rv.Add(TransactionItemViewModel.Create(item, _transaction, expenseCategory));
            }
            return rv;
        }

        //public async void HandleEvent(TransactionItemEvent @event)
        //{
        //    await ReloadItemsAsync();
        //}
    }
}