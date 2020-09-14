using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class TransactionsViewModel :
        CollectionViewModelBase<ITransactionItem>,
        IEventListener<TransactionEvent>,
        IEventListener<TransactionItemEvent>
    {
        public TransactionsViewModel()
        {
            _view.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
            _view.SortDescriptions.Add(new SortDescription("TransactionID", ListSortDirection.Descending));

            NotificationCenter.Register<TransactionEvent>(this);
            NotificationCenter.Register<TransactionItemEvent>(this);
        }

        public ICollectionView TransactionsView
        {
            get { return _view; }
        }

        public string Title
        {
            get { return "Transaction History"; }
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

        protected override async Task<IEnumerable<ITransactionItem>> GetItems()
        {
            var rv = new List<ITransactionItem>();
            var transactions = await GetDatabaseConnection()
                                         .Table<Transaction>()
                                         .Where(x => x.Date >= QueryStart && x.Date <= QueryEnd)
                                         .ToListAsync();
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var transaction in transactions)
                rv.Add(await TransactionViewModel.Create(transaction));
            // ReSharper restore LoopCanBeConvertedToQuery

            var transfers = await GetDatabaseConnection()
                                      .Table<Transfer>()
                                      .Where(x => x.Date >= QueryStart && x.Date <= QueryEnd)
                                      .ToListAsync();

            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var transfer in transfers)
                rv.Add(await TransferViewModel.Create(transfer));
            // ReSharper restore LoopCanBeConvertedToQuery

            return rv;
        }

        public async void HandleEvent(TransactionEvent @event)
        {
            await ReloadItemsAsync();
        }

        public async void HandleEvent(TransactionItemEvent @event)
        {
            await ReloadItemsAsync();
        }
    }
}