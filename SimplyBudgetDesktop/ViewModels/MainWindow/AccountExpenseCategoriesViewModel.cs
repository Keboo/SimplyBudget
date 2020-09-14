using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class AccountExpenseCategoriesViewModel : CollectionViewModelBase<ExpenseCategoryViewModel>, IEventListener<ExpenseCategoryEvent>
    {
        private readonly int _accountID;
        private string _accountName;

        public AccountExpenseCategoriesViewModel(int accountID)
        {
            _accountID = accountID;
        }

        public ICollectionView ExpenseCategoriesView
        {
            get { return _view; }
        }

        public string Title
        {
            get { return "Expense Categories for " + _accountName; }
        }

        protected override async Task<IEnumerable<ExpenseCategoryViewModel>> GetItems()
        {
            var account = await DatabaseManager.GetAsync<Account>(_accountID);
            if (account == null) return null;
            _accountName = account.Name;
            RaisePropertyChanged(() => Title);

            var expenseCategories = await GetDatabaseConnection().Table<ExpenseCategory>().Where(x => x.AccountID == _accountID).ToListAsync();
            return expenseCategories.Select(ExpenseCategoryViewModel.Create);
        }

        public async void HandleEvent(ExpenseCategoryEvent @event)
        {
            await ReloadItemsAsync();
        }
    }
}