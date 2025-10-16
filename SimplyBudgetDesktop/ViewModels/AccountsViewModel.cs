using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using System.ComponentModel;
using System.Windows.Input;

namespace SimplyBudget.ViewModels;

public class AccountsViewModel :
    CollectionViewModelBase<AccountViewModel>,
    IRecipient<DatabaseEvent<Account>>, 
    IRecipient<DatabaseEvent<ExpenseCategory>>
{
    public ICommand ShowAccountsCommand { get; }
    
    private IDataClient DataClient { get; }

    public ICollectionView AccountsView => _view;

    public AccountsViewModel(IDataClient dataClient, IMessenger messenger)
    {
        DataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
        messenger.Register<DatabaseEvent<Account>>(this);
        messenger.Register<DatabaseEvent<ExpenseCategory>>(this);
        ShowAccountsCommand = new RelayCommand(OnShowAccounts);
    }

    protected override async IAsyncEnumerable<AccountViewModel> GetItems()
    {
        await foreach (var account in DataClient.Accounts.GetAllAsync())
        {
            yield return await AccountViewModel.Create(DataClient.ExpenseCategories, account);
        }
    }

    public async void Receive(DatabaseEvent<Account> _) => await LoadItemsAsync();

    public async void Receive(DatabaseEvent<ExpenseCategory> _) => await LoadItemsAsync();

    private async void OnShowAccounts()
    {
        await DialogHost.Show(this);
    }
}
