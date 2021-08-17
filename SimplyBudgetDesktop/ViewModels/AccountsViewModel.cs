using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class AccountsViewModel :
        CollectionViewModelBase<AccountViewModel>,
        IRecipient<AccountEvent>, 
        IRecipient<ExpenseCategoryEvent>
    {
        public ICommand ShowAccountsCommand { get; }
        
        private BudgetContext Context { get; }

        public ICollectionView AccountsView => _view;

        public AccountsViewModel(BudgetContext context, IMessenger messenger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            messenger.Register<AccountEvent>(this);
            messenger.Register<ExpenseCategoryEvent>(this);
            ShowAccountsCommand = new RelayCommand(OnShowAccounts);
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

        private async void OnShowAccounts()
        {
            await DialogHost.Show(this);
        }
    }
}
