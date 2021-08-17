using Microsoft.Toolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Windows;

namespace SimplyBudget.ViewModels
{
    public class ExpenseCategoryViewModel : ObservableObject, IClipboardData
    {
        public static ExpenseCategoryViewModel Create(ExpenseCategory expenseCategory)
        {
            if (expenseCategory is null) throw new ArgumentNullException(nameof(expenseCategory));
            var rv = new ExpenseCategoryViewModel(expenseCategory.ID);
            SetProperties(expenseCategory, rv);
            return rv;
        }

        protected static void SetProperties(ExpenseCategory expenseCategory, ExpenseCategoryViewModel viewModel)
        {
            if (expenseCategory is null) throw new ArgumentNullException(nameof(expenseCategory));
            if (viewModel is null) throw new ArgumentNullException(nameof(viewModel));
            viewModel.Name = expenseCategory.Name;
            viewModel.Balance = expenseCategory.CurrentBalance;
            viewModel.BudgetedAmount = expenseCategory.BudgetedAmount;
            viewModel.BudgetedPercentage = expenseCategory.BudgetedPercentage;
            viewModel.CategoryName = expenseCategory.CategoryName ?? string.Empty;
            viewModel.IsHidden = expenseCategory.IsHidden;
            viewModel.Cap = expenseCategory.Cap;
        }

        public void OnCopy()
        {
            Clipboard.SetText(Balance.FormatCurrency());
        }

        public ExpenseCategoryViewModel(int expenseCategoryID)
        {
            ExpenseCategoryID = expenseCategoryID;
        }

        public int ExpenseCategoryID { get; }

        private string? _name;
        public string? Name
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

        private string? _categoryName;
        public string? CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }

        private bool _isHidden;
        public bool IsHidden
        {
            get => _isHidden;
            set => SetProperty(ref _isHidden, value);
        }

        private int? _cap;
        public int? Cap
        {
            get => _cap;
            set => SetProperty(ref _cap, value);
        }
    }
}