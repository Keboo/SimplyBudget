using CommunityToolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels;

public class AccountViewModel : ObservableObject
{
    public static async Task<AccountViewModel> Create(IDataClient<ExpenseCategory> expenseCategories, Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        var currentAmount = await expenseCategories.GetCurrentAmountAsync(account.ID, CancellationToken.None);
        return new AccountViewModel(account.ID)
        {
            Name = account.Name,
            LastValidatedDate = account.ValidatedDate,
            IsDefault = account.IsDefault,
            CurrentAmount = currentAmount
        };
    }

    private AccountViewModel(int accountID)
    {
        AccountID = accountID;
    }

    public int AccountID { get; }

    private string? _name;
    public string? Name
    {
        get => _name;
        set => _ = SetProperty(ref _name, value);
    }

    private DateTime _lastValidatedDate;
    public DateTime LastValidatedDate
    {
        get => _lastValidatedDate;
        set => SetProperty(ref _lastValidatedDate, value);
    }

    private int _currentAmount;
    public int CurrentAmount
    {
        get => _currentAmount;
        set => SetProperty(ref _currentAmount, value);
    }

    private bool _isDefault;
    public bool IsDefault
    {
        get => _isDefault;
        set => SetProperty(ref _isDefault, value);
    }
}