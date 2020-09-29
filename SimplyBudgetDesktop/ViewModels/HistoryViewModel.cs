using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SimplyBudget.ViewModels.MainWindow
{
    public class HistoryViewModel : CollectionViewModelBase<BudgetHistoryViewModel>
    {
        public BudgetContext Context { get; }

        private DateTime OldestTime { get; set; }

        public ICollectionView HistoryView => _view;

        public HistoryViewModel(BudgetContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            OldestTime = DateTime.Now.AddMonths(-2).StartOfMonth();
            Sort(nameof(BudgetHistoryViewModel.Date), ListSortDirection.Descending);
        }

        protected override async IAsyncEnumerable<BudgetHistoryViewModel> GetItems()
        {
            await foreach(var item in Context.Incomes.Where(x => x.Date >= OldestTime).AsAsyncEnumerable())
            {
                yield return BudgetHistoryViewModel.Create(item);
            }
            await foreach (var item in Context.Transactions.Where(x => x.Date >= OldestTime).AsAsyncEnumerable())
            {
                int total = Context.TransactionItems.Where(x => x.TransactionID == item.ID).Select(x => x.Amount).Sum();
                yield return BudgetHistoryViewModel.Create(item, total);
            }
        }
    }
}