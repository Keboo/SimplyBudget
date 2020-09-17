using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using SimplyBudget.Collections;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class EditAccountViewModel : ViewEditViewModel<Account>, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly RelayCommand _markAsValidatedCommand;
        private Account _existingAccount;

        private BudgetContext Context { get; } = BudgetContext.Instance;
        
        public EditAccountViewModel()
        {
            _selectedItems = new ArrayList();
            _markAsValidatedCommand = new RelayCommand(() => LastValidatedDate = DateTime.Today);

            ExpenseCategories = CollectionViewSource.GetDefaultView(ExpenseCategoryCollection.Instance);

            ExpenseCategories.SortDescriptions.Add(new SortDescription(nameof(ExpenseCategory.Name), ListSortDirection.Ascending));
        }

        public ICollectionView ExpenseCategories { get; }

        public ICommand MarkAsValidatedCommand => _markAsValidatedCommand;

        private IList _selectedItems;
        public IList SelectedItems
        {
            get => _selectedItems;
            set => SetProperty(ref _selectedItems, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    NameError = "";
            }
        }

        private string _nameError;
        public string NameError
        {
            get => _nameError;
            set => SetProperty(ref _nameError, value);
        }

        private DateTime _lastValidatedDate;
        public DateTime LastValidatedDate
        {
            get => _lastValidatedDate;
            set => SetProperty(ref _lastValidatedDate, value);
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }

        protected override async Task CreateAsync()
        {
            if (HasErrors()) return;
            
            //Copy the selected items
            var selectedExpenseCategories = new List<ExpenseCategory>(_selectedItems.OfType<ExpenseCategory>());

            var account = new Account
            {
                Name = Name,
                //IsDefault = IsDefault,
                ValidatedDate = DateTime.Today
            };
            Context.Accounts.Add(account);
            await Context.SaveChangesAsync();
            if (IsDefault)
            {
                await Context.SetAsDefaultAsync(account);
            }

            foreach (var expenseCategory in selectedExpenseCategories)
            {
                expenseCategory.AccountID = account.ID;
                await Context.SaveChangesAsync();
            }

            RequestClose.Raise(this, EventArgs.Empty);
        }

        protected override async Task SaveAsync()
        {
            if (HasErrors()) return;

            _existingAccount.Name = Name;
            //_existingAccount.IsDefault = IsDefault;
            _existingAccount.ValidatedDate = LastValidatedDate;
            await BudgetContext.Instance.SaveChangesAsync();
            if (IsDefault)
            {
                await BudgetContext.Instance.SetAsDefaultAsync(_existingAccount);
            }


            //Copy the selected items
            var selectedExpenseCategories = new HashSet<ExpenseCategory>(_selectedItems.OfType<ExpenseCategory>());

            var modifiedItems = new HashSet<ExpenseCategory>(selectedExpenseCategories);
            modifiedItems.SymmetricExceptWith(await BudgetContext.Instance.ExpenseCategories.Where(x => x.AccountID == _existingAccount.ID).ToListAsync());

            //Get a fallback account id to move the items to
            int fallbackAccountID = 0;
            if (_existingAccount.IsDefault == false)
            {
                var defaultAccount = await Account.GetDefault();
                if (defaultAccount != null)
                    fallbackAccountID = defaultAccount.ID;
            }

            foreach (var expenseCategory in modifiedItems)
            {
                expenseCategory.AccountID = selectedExpenseCategories.Contains(expenseCategory) ? _existingAccount.ID : fallbackAccountID;
                await expenseCategory.Save();
            }

            RequestClose.Raise(this, EventArgs.Empty);
        }

        private bool HasErrors()
        {
            bool rv = false;

            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "A name is required";
                rv = true;
            }
            return rv;
        }

        protected override async Task SetPropertiesToEditAsync(Account account)
        {
            Name = account.Name;
            LastValidatedDate = account.ValidatedDate;
            IsDefault = account.IsDefault;

            var expenseCategories = await BudgetContext.Instance.ExpenseCategories.Where(x => x.AccountID == account.ID).ToListAsync();
            if (expenseCategories != null && expenseCategories.Count > 0)
            {
                var arrayList = new ArrayList(expenseCategories.Count);
                foreach (var item in expenseCategories)
                    arrayList.Add(item);
                SelectedItems = arrayList;
            }
            _existingAccount = account;
        }
    }

    
}