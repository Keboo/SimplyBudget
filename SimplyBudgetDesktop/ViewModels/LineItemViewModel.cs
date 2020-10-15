using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class LineItemViewModel : ObservableObject
    {
        public ICommand SetAmountCommand { get; }

        public IList<ExpenseCategory> ExpenseCategories { get; }

        public LineItemViewModel(IList<ExpenseCategory> expenseCategories)
        {
            ExpenseCategories = expenseCategories ?? throw new ArgumentNullException(nameof(expenseCategories));

            SetAmountCommand = new RelayCommand(OnSetAmount);
        }

        private void OnSetAmount()
        {
            Amount = DesiredAmount;
        }

        private int _amount;
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }


        private int _desiredAmount;
        public int DesiredAmount
        {
            get => _desiredAmount;
            set => SetProperty(ref _desiredAmount, value);
        }

        private ExpenseCategory _selectedCategory;
        public ExpenseCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }
    }

}
