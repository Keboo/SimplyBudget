using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using SimplyBudget.Collections;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class EditAccountViewModel : ViewEditViewModel<Account>, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly ICollectionView _expenseCategories;
        private readonly DelegateCommand _marAsValidatedCommand;
        private Account _existingAccount;
        
        public EditAccountViewModel()
        {
            _selectedItems = new ArrayList();
            _marAsValidatedCommand = new DelegateCommand(() => LastValidatedDate = DateTime.Today);

            _expenseCategories = CollectionViewSource.GetDefaultView(ExpenseCategoryCollection.Instance);

            _expenseCategories.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public ICollectionView ExpenseCategories
        {
            get { return _expenseCategories; }
        }

        public ICommand MarkAsValidatedCommand
        {
            get { return _marAsValidatedCommand; }
        }

        private IList _selectedItems;
        public IList SelectedItems
        {
            get { return _selectedItems; }
            set { SetProperty(ref _selectedItems, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (SetProperty(ref _name, value))
                    NameError = "";
            }
        }

        private string _nameError;
        public string NameError
        {
            get { return _nameError; }
            set { SetProperty(ref _nameError, value); }
        }

        private DateTime _lastValidatedDate;
        public DateTime LastValidatedDate
        {
            get { return _lastValidatedDate; }
            set { SetProperty(ref _lastValidatedDate, value); }
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get { return _isDefault; }
            set { SetProperty(ref _isDefault, value); }
        }

        protected override async Task CreateAsync()
        {
            if (HasErrors()) return;
            
            //Copy the selected items
            var selectedExpenseCategories = new List<ExpenseCategory>(_selectedItems.OfType<ExpenseCategory>());

            var account = new Account
            {
                Name = Name,
                IsDefault = IsDefault,
                ValidatedDate = DateTime.Today
            };
            await account.Save();

            foreach (var expenseCategory in selectedExpenseCategories)
            {
                expenseCategory.AccountID = account.ID;
                await expenseCategory.Save();
            }

            RequestClose.Raise(this, EventArgs.Empty);
        }

        protected override async Task SaveAsync()
        {
            if (HasErrors()) return;

            _existingAccount.Name = Name;
            _existingAccount.IsDefault = IsDefault;
            _existingAccount.ValidatedDate = LastValidatedDate;
            await _existingAccount.Save();

            //Copy the selected items
            var selectedExpenseCategories = new HashSet<ExpenseCategory>(_selectedItems.OfType<ExpenseCategory>());

            var modifiedItems = new HashSet<ExpenseCategory>(selectedExpenseCategories);
            modifiedItems.SymmetricExceptWith(await _existingAccount.GetExpenseCategories());

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

            var expenseCategories = await account.GetExpenseCategories();
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