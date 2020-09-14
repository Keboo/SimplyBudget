using System;
using System.Globalization;
using System.Windows;
using SimplyBudget.ViewModels.MainWindow;

namespace SimplyBudget.ValueConverter
{
    public class ExpenseCategoryBudgettedAmountReachedValueConverter : MarkupValueConverter<ExpenseCategoryBudgettedAmountReachedValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public ExpenseCategoryBudgettedAmountReachedValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var expenseCategoryVM = value as ExpenseCategoryViewModelEx;
            if (expenseCategoryVM != null)
            {
                if (expenseCategoryVM.BudgetedPercentage > 0)
                    return DependencyProperty.UnsetValue; //Returning nothing for percentages since I don't know how to properly handle them
                var rv = expenseCategoryVM.MonthlyAllocations.CompareTo(expenseCategoryVM.BudgetedAmount);
                return Math.Max(-1, Math.Min(1, rv));
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
