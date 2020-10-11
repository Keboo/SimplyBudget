using Microsoft.EntityFrameworkCore;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class TransactionsViewModel :
        CollectionViewModelBaseOld<ITransactionItem>
    {
        private BudgetContext Context { get; } = BudgetContext.Instance;

        public TransactionsViewModel()
        {
            _view.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
            _view.SortDescriptions.Add(new SortDescription("TransactionID", ListSortDirection.Descending));

            //NotificationCenter.Register<TransactionEvent>(this);
            //NotificationCenter.Register<TransactionItemEvent>(this);
        }

        public ICollectionView TransactionsView => _view;

        public string Title => "Transaction History";

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

        protected override async Task<IEnumerable<ITransactionItem>> GetItems()
        {
            var rv = new List<ITransactionItem>();
            var transactions = await Context.Transactions
                                         .Where(x => x.Date >= QueryStart && x.Date <= QueryEnd)
                                         .ToListAsync();
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var transaction in transactions)
                rv.Add(await TransactionViewModel.Create(Context, transaction));
            // ReSharper restore LoopCanBeConvertedToQuery

            var transfers = await Context.Transfers
                                      .Where(x => x.Date >= QueryStart && x.Date <= QueryEnd)
                                      .ToListAsync();

            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var transfer in transfers)
                rv.Add(await TransferViewModel.Create(transfer));
            // ReSharper restore LoopCanBeConvertedToQuery

            return rv;
        }

        //public async void HandleEvent(TransactionEvent @event)
        //{
        //    await ReloadItemsAsync();
        //}

        //public async void HandleEvent(TransactionItemEvent @event)
        //{
        //    await ReloadItemsAsync();
        //}
    }
}