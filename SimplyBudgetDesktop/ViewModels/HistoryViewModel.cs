using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
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
        IRecipient<IncomeItemEvent>,
        IRecipient<TransferEvent>,
        IRecipient<CurrentMonthChanged>
    {
        public BudgetContext Context { get; }
        public ICurrentMonth CurrentMonth { get; }

        public ICollectionView HistoryView => _view;

        public HistoryViewModel(BudgetContext context, IMessenger messenger, ICurrentMonth currentMonth)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
            Sort(nameof(BudgetHistoryViewModel.Date), ListSortDirection.Descending);

            messenger.Register<TransactionEvent>(this);
            messenger.Register<TransactionItemEvent>(this);
            messenger.Register<IncomeEvent>(this);
            messenger.Register<IncomeItemEvent>(this);
            messenger.Register<TransferEvent>(this);
            messenger.Register<CurrentMonthChanged>(this);
        }

        protected override async IAsyncEnumerable<BudgetHistoryViewModel> GetItems()
        {
            var oldestTime = CurrentMonth.CurrenMonth.AddMonths(-2).StartOfMonth();

            await foreach(var item in Context.Incomes.Where(x => x.Date >= oldestTime).AsAsyncEnumerable())
            {
                yield return BudgetHistoryViewModel.Create(item);
            }
            await foreach (var item in Context.Transactions.Where(x => x.Date >= oldestTime).AsAsyncEnumerable())
            {
                int total = Context.TransactionItems.Where(x => x.TransactionID == item.ID).Select(x => x.Amount).Sum();
                yield return BudgetHistoryViewModel.Create(item, total);
            }
            await foreach(var item in Context.Transfers.Where(x => x.Date >= oldestTime).AsAsyncEnumerable())
            {
                var from = await Context.FindAsync<ExpenseCategory>(item.FromExpenseCategoryID);
                var to = await Context.FindAsync<ExpenseCategory>(item.ToExpenseCategoryID);
                yield return BudgetHistoryViewModel.Create(item, from, to);
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

        public void Receive(TransferEvent message) => LoadItemsAsync();

        public void Receive(CurrentMonthChanged message) => LoadItemsAsync();
    }
}