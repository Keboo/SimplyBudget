using Microsoft.EntityFrameworkCore;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class AccountExpenseCategoriesViewModel : CollectionViewModelBaseOld<ExpenseCategoryViewModel>
    {
        private readonly int _accountID;
        private string _accountName;

        private BudgetContext Context { get; } = BudgetContext.Instance;

        public AccountExpenseCategoriesViewModel(int accountID)
        {
            _accountID = accountID;
        }

        public ICollectionView ExpenseCategoriesView => _view;

        public string Title => "Expense Categories for " + _accountName;

        protected override async Task<IEnumerable<ExpenseCategoryViewModel>> GetItems()
        {
            var account = await Context.Accounts.FindAsync(_accountID);
            if (account is null) return null;
            _accountName = account.Name;
            OnPropertyChanged(nameof(Title));

            var expenseCategories = await Context.ExpenseCategories.Where(x => x.AccountID == _accountID).ToListAsync();
            return expenseCategories.Select(ExpenseCategoryViewModel.Create);
        }

    //    public async void HandleEvent(ExpenseCategoryEvent @event)
    //    {
    //        await ReloadItemsAsync();
    //    }
    }
}