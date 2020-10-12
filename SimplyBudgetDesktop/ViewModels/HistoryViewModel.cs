using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{

    public class HistoryViewModel : CollectionViewModelBase<BudgetHistoryViewModel>,
        IRecipient<TransactionEvent>,
        IRecipient<TransactionItemEvent>,
        IRecipient<IncomeEvent>,
        IRecipient<IncomeItemEvent>
    {
        public BudgetContext Context { get; }

        private DateTime OldestTime { get; set; }


        public ICollectionView HistoryView => _view;

        public HistoryViewModel(BudgetContext context, IMessenger messenger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            OldestTime = DateTime.Now.AddMonths(-2).StartOfMonth();
            Sort(nameof(BudgetHistoryViewModel.Date), ListSortDirection.Descending);

            messenger.Register<TransactionEvent>(this);
            messenger.Register<TransactionItemEvent>(this);
            messenger.Register<IncomeEvent>(this);
            messenger.Register<IncomeItemEvent>(this);
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

        public async Task DeleteItems(IEnumerable<BudgetHistoryViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach(var item in items.ToList())
            {
                await item.Delete(Context);
            }
        }

        public void Receive(IncomeItemEvent message) => LoadItemsAsync();

        public void Receive(IncomeEvent message) => LoadItemsAsync();

        public void Receive(TransactionEvent message) => LoadItemsAsync();

        public void Receive(TransactionItemEvent message) => LoadItemsAsync();
    }
}