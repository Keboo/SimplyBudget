using System;
using JetBrains.Annotations;
using SimplyBudgetShared.Data;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.Data
{
    internal class ExpenseCategoryViewModel : ViewModelBase, IDatabaseItem
    {
        public static ExpenseCategoryViewModel Create(ExpenseCategory expenseCategory)
        {
            if (expenseCategory is null) throw new ArgumentNullException("expenseCategory");
            var rv = new ExpenseCategoryViewModel(expenseCategory.ID);
            SetProperties(expenseCategory, rv);
            return rv;
        }

        protected static void SetProperties(ExpenseCategory expenseCategory, ExpenseCategoryViewModel viewModel)
        {
            if (expenseCategory is null) throw new ArgumentNullException("expenseCategory");
            if (viewModel is null) throw new ArgumentNullException("viewModel");
            viewModel.Name = expenseCategory.Name;
            viewModel.Balance = expenseCategory.CurrentBalance;
            viewModel.BudgetedAmount = expenseCategory.BudgetedAmount;
            viewModel.BudgetedPercentage = expenseCategory.BudgetedPercentage;
            viewModel.CategoryName = expenseCategory.CategoryName ?? string.Empty;
        }

        public ExpenseCategoryViewModel(int expenseCategoryID)
        {
            ExpenseCategoryID = expenseCategoryID;
        }

        public int ExpenseCategoryID { get; }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _balance;
        public int Balance
        {
            get => _balance;
            set => SetProperty(ref _balance, value);
        }

        private int _budgetedAmount;
        public int BudgetedAmount
        {
            get => _budgetedAmount;
            set => SetProperty(ref _budgetedAmount, value);
        }

        private int _budgetedPercentage;
        public int BudgetedPercentage
        {
            get => _budgetedPercentage;
            set => SetProperty(ref _budgetedPercentage, value);
        }

        private string _categoryName;
        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }

        async Task<BaseItem> IDatabaseItem.GetItem()
        {
            return await DatabaseManager.GetAsync<ExpenseCategory>(ExpenseCategoryID);
        }
    }
}