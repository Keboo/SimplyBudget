using System;
using JetBrains.Annotations;
using SimplyBudgetShared.Data;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.Data
{
    internal class ExpenseCategoryViewModel : ViewModelBase, IDatabaseItem
    {
        public static ExpenseCategoryViewModel Create([NotNull] ExpenseCategory expenseCategory)
        {
            if (expenseCategory == null) throw new ArgumentNullException("expenseCategory");
            var rv = new ExpenseCategoryViewModel(expenseCategory.ID);
            SetProperties(expenseCategory, rv);
            return rv;
        }

        protected static void SetProperties([NotNull] ExpenseCategory expenseCategory, [NotNull] ExpenseCategoryViewModel viewModel)
        {
            if (expenseCategory == null) throw new ArgumentNullException("expenseCategory");
            if (viewModel == null) throw new ArgumentNullException("viewModel");
            viewModel.Name = expenseCategory.Name;
            viewModel.Balance = expenseCategory.CurrentBalance;
            viewModel.BudgetedAmount = expenseCategory.BudgetedAmount;
            viewModel.BudgetedPercentage = expenseCategory.BudgetedPercentage;
            viewModel.CategoryName = expenseCategory.CategoryName ?? string.Empty;
        }

        private readonly int _expenseCategoryID;

        public ExpenseCategoryViewModel(int expenseCategoryID)
        {
            _expenseCategoryID = expenseCategoryID;
        }

        public int ExpenseCategoryID
        {
            get { return _expenseCategoryID; }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private int _balance;
        public int Balance
        {
            get { return _balance; }
            set { SetProperty(ref _balance, value); }
        }

        private int _budgetedAmount;
        public int BudgetedAmount
        {
            get { return _budgetedAmount; }
            set { SetProperty(ref _budgetedAmount, value); }
        }

        private int _budgetedPercentage;
        public int BudgetedPercentage
        {
            get { return _budgetedPercentage; }
            set { SetProperty(ref _budgetedPercentage, value); }
        }

        private string _categoryName;
        public string CategoryName
        {
            get { return _categoryName; }
            set { SetProperty(ref _categoryName, value); }
        }

        async Task<BaseItem> IDatabaseItem.GetItem()
        {
            return await DatabaseManager.GetAsync<ExpenseCategory>(ExpenseCategoryID);
        }
    }
}