using SimplyBudgetShared.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.Data
{
    public class AccountViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject, IDatabaseItem
    {
        public static async Task<AccountViewModel> Create(BudgetContext context, Account account)
        {
            if (account is null) throw new ArgumentNullException(nameof(account));
            var currentAmount = await context.GetCurrentAmount(account.ID);
            return new AccountViewModel(account.ID)
                       {
                           Name = account.Name,
                           LastValidatedDate = account.ValidatedDate,
                           IsDefault = account.IsDefault,
                           CurrentAmount = currentAmount
                       };
        }

        public static AccountViewModel CreateEmpty()
        {
            return new AccountViewModel(0);
        }

        private AccountViewModel(int accountID)
        {
            AccountID = accountID;
        }

        public int AccountID { get; }

        private string _name;
        public string Name
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

        public async Task<BaseItem> GetItem()
        {
            return await BudgetContext.Instance.Accounts.FindAsync(AccountID);
        }
    }
}