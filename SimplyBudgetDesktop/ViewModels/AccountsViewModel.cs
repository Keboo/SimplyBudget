using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SimplyBudget.ViewModels
{
    public class AccountsViewModel :
        CollectionViewModelBase<AccountViewModel>,
        IRecipient<AccountEvent>, 
        IRecipient<ExpenseCategoryEvent>
    {
        public BudgetContext Context { get; }

        public ICollectionView AccountsView => _view;

        public AccountsViewModel(BudgetContext context, IMessenger messenger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected override async IAsyncEnumerable<AccountViewModel> GetItems()
        {
            await foreach(var account in Context.Accounts)
            {
                yield return await AccountViewModel.Create(Context, account);
            }
        }

        public void Receive(AccountEvent message) => LoadItemsAsync();

        public void Receive(ExpenseCategoryEvent message) => LoadItemsAsync();
    }
}
