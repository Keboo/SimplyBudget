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
        IRecipient<DatabaseEvent<Account>>, 
        IRecipient<DatabaseEvent<ExpenseCategory>>
    {
        public ICommand ShowAccountsCommand { get; }
        
        private Func<BudgetContext> ContextFactory { get; }

        public ICollectionView AccountsView => _view;

        public AccountsViewModel(Func<BudgetContext> contextFactory, IMessenger messenger)
        {
            ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            messenger.Register<DatabaseEvent<Account>>(this);
            messenger.Register<DatabaseEvent<ExpenseCategory>>(this);
            ShowAccountsCommand = new RelayCommand(OnShowAccounts);
        }

        protected override async IAsyncEnumerable<AccountViewModel> GetItems()
        {
            using var context = ContextFactory();
            await foreach (var account in context.Accounts)
            {
                yield return await AccountViewModel.Create(context, account);
            }
        }

        public void Receive(DatabaseEvent<Account> _) => LoadItemsAsync();

        public void Receive(DatabaseEvent<ExpenseCategory> _) => LoadItemsAsync();

        private async void OnShowAccounts()
        {
            await DialogHost.Show(this);
        }
    }
}
