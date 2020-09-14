using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels.Data
{
    internal class AccountViewModel : ViewModelBase, IDatabaseItem
    {
        public static async Task<AccountViewModel> Create([NotNull] Account account)
        {
            if (account == null) throw new ArgumentNullException("account");
            var currentAmount = await account.GetCurrentAmount();
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

        private readonly int _accountID;

        private AccountViewModel(int accountID)
        {
            _accountID = accountID;
        }

        public int AccountID
        {
            get { return _accountID; }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private DateTime _lastValidatedDate;
        public DateTime LastValidatedDate
        {
            get { return _lastValidatedDate; }
            set { SetProperty(ref _lastValidatedDate, value); }
        }

        private int _currentAmount;
        public int CurrentAmount
        {
            get { return _currentAmount; }
            set { SetProperty(ref _currentAmount, value); }
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get { return _isDefault; }
            set { SetProperty(ref _isDefault, value); }
        }

        public async Task<BaseItem> GetItem()
        {
            return await DatabaseManager.Instance.Connection.GetAsync<Account>(AccountID);
        }
    }
}