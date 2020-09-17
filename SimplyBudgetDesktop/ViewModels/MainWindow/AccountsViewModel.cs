using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class AccountsViewModel : CollectionViewModelBase<AccountViewModel>, 
        IEventListener<ExpenseCategoryEvent>, IEventListener<AccountEvent>
    {
        private BudgetContext Context { get; } = BudgetContext.Instance;

        public AccountsViewModel()
        {
            _view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            NotificationCenter.Register<ExpenseCategoryEvent>(this);
            NotificationCenter.Register<AccountEvent>(this);
        }

        public ICollectionView AccountsView => _view;

        public string Title => "Account Information";

        protected override async Task<IEnumerable<AccountViewModel>> GetItems()
        {
            var accounts = await Context.Accounts.ToListAsync();
            var rv = new List<AccountViewModel>();
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var account in accounts)
                rv.Add(await AccountViewModel.Create(Context, account));
            // ReSharper restore LoopCanBeConvertedToQuery
            return rv;
        }

        public async void HandleEvent(ExpenseCategoryEvent @event)
        {
            await ReloadItemsAsync();
        }

        public async void HandleEvent(AccountEvent @event)
        {
            await ReloadItemsAsync();
        }
    }
}