using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using SimplyBudget.Utilities;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using System.Collections.ObjectModel;
using System.Linq;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class EditExpenseCategoryViewModel : ViewEditViewModel<ExpenseCategory>, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly ObservableCollection<string> _categories;
        private readonly ObservableCollection<AccountViewModel> _accounts;
        private readonly DelegateCommand _toggleCreateCommand;
        private readonly DelegateCommand _toggleUsePercentageCommand;
        private readonly DelegateCommand _editAmountCommand;

        private ExpenseCategory _existingExpenseCategory;

        public EditExpenseCategoryViewModel()
        {
            _toggleCreateCommand = new DelegateCommand(OnToggleCreate);
            _toggleUsePercentageCommand = new DelegateCommand(OnToggleUsePercentage);
            _editAmountCommand = new DelegateCommand(() => EditingAmount = true);

            _categories = new ObservableCollection<string>();
            _accounts = new ObservableCollection<AccountViewModel>();
            if (DesignerHelper.IsDesignMode == false)
            {
                LoadCategories();
                LoadAccounts();
            }
        }

        public ICommand ToggleCreateCommand
        {
            get { return _toggleCreateCommand; }
        }

        public ICommand TogglePercentageCommand
        {
            get { return _toggleUsePercentageCommand; }
        }

        public ICommand EditAmountCommand
        {
            get { return _editAmountCommand; }
        }

        public ObservableCollection<string> Categories
        {
            get { return _categories; }
        }

        public ObservableCollection<AccountViewModel> Accounts
        {
            get { return _accounts; }
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

        private int _budgetedAmount;
        public int BudgetedAmount
        {
            get { return _budgetedAmount; }
            set
            {
                if (SetProperty(ref _budgetedAmount, value))
                    AmountError = "";
            }
        }

        private string _amountError;
        public string AmountError
        {
            get { return _amountError; }
            set { SetProperty(ref _amountError, value); }
        }

        private string _categoryName;
        public string CategoryName
        {
            get { return _categoryName; }
            set { SetProperty(ref _categoryName, value); }
        }

        private bool _isCreateCategory;
        public bool IsCreateCategory
        {
            get { return _isCreateCategory; }
            set { SetProperty(ref _isCreateCategory, value); }
        }

        private int? _selectedAccountId;
        public int? SelectedAccountID
        {
            get { return _selectedAccountId; }
            set { SetProperty(ref _selectedAccountId, value); }
        }

        private bool _isPercentage;
        public bool IsPercentage
        {
            get { return _isPercentage; }
            set
            {
                if (SetProperty(ref _isPercentage, value))
                    AmountError = "";
            }
        }

        private int _percentage;
        public int Percentage
        {
            get { return _percentage; }
            set
            {
                if (SetProperty(ref _percentage, value))
                    AmountError = "";
            }
        }

        private bool _editingAmount = true;
        public bool EditingAmount
        {
            get { return _editingAmount; }
            set { SetProperty(ref _editingAmount, value); }
        }

        private int _currentBalance;
        public int CurrentBalance
        {
            get { return _currentBalance; }
            set { SetProperty(ref _currentBalance, value); }
        }

        protected async override Task CreateAsync()
        {
            if (HasErrors()) return;

            var name = Name;
            var amount = BudgetedAmount;
            var percentage = Percentage;

            var expenseCategory = new ExpenseCategory
                                      {
                                          Name = name,
                                          BudgetedAmount = IsPercentage ? 0 : amount,
                                          BudgetedPercentage = IsPercentage ? percentage : 0,
                                          CategoryName = CategoryName,
                                          AccountID = SelectedAccountID ?? 0,
                                          CurrentBalance = CurrentBalance
                                      };
            await expenseCategory.Save();

            RequestClose.Raise(this, EventArgs.Empty);
        }

        protected override async Task SaveAsync()
        {
            if (_existingExpenseCategory == null) return;

            if (HasErrors()) return;

            var name = Name;
            var amount = BudgetedAmount;
            var percentage = Percentage;

            _existingExpenseCategory.Name = name;
            _existingExpenseCategory.BudgetedAmount = IsPercentage ? 0 : amount;
            _existingExpenseCategory.BudgetedPercentage = IsPercentage ? percentage : 0;
            _existingExpenseCategory.CategoryName = CategoryName;
            _existingExpenseCategory.AccountID = SelectedAccountID ?? 0;
            _existingExpenseCategory.CurrentBalance = CurrentBalance;

            await _existingExpenseCategory.Save();
            RequestClose.Raise(this, EventArgs.Empty);
        }

        private bool HasErrors()
        {
            bool rv = false;
            var name = Name;
            
            if (string.IsNullOrWhiteSpace(name))
            {
                NameError = "A name is required";
                rv = true;
            }

            return rv;
        }

        protected override Task SetPropertiesToEditAsync(ExpenseCategory expenseCategory)
        {
            Name = expenseCategory.Name;
            BudgetedAmount = expenseCategory.BudgetedAmount;
            CategoryName = expenseCategory.CategoryName;
            SelectedAccountID = expenseCategory.AccountID;
            Percentage = expenseCategory.BudgetedPercentage;
            IsPercentage = expenseCategory.UsePercentage;
            CurrentBalance = expenseCategory.CurrentBalance;
            EditingAmount = false;
            _existingExpenseCategory = expenseCategory;
            
            return Task.FromResult<object>(null);
        }

        private async void LoadCategories()
        {
            var expenseCategories = await GetDatabaseConnection().Table<ExpenseCategory>().ToListAsync();
            _categories.Clear();
            _categories.AddRange(expenseCategories.Select(x => x.CategoryName).Distinct().OrderBy(x => x));

            if (string.IsNullOrWhiteSpace(CategoryName))
                CategoryName = _categories.FirstOrDefault();
        }

        private async void LoadAccounts()
        {
            var accounts = await GetDatabaseConnection().Table<Account>().ToListAsync();
            _accounts.Clear();

            var none = AccountViewModel.CreateEmpty();
            none.Name = "None";
            _accounts.Add(none);

            foreach (var account in accounts.OrderBy(x => x.IsDefault).ThenBy(x => x.Name))
            {
                _accounts.Add(await AccountViewModel.Create(account));
            }

            if (SelectedAccountID == null)
            {
                var account = _accounts.FirstOrDefault(x => x.IsDefault) ?? none;
                SelectedAccountID = account.AccountID;
            }
                
        }

        private void OnToggleCreate()
        {
            IsCreateCategory = IsCreateCategory == false;
        }

        private void OnToggleUsePercentage()
        {
            IsPercentage = IsPercentage == false;
        }
    }
}