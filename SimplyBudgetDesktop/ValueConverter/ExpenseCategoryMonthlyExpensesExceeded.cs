using System;
using System.Globalization;
using System.Windows;
using SimplyBudget.ViewModels;

namespace SimplyBudget.ValueConverter;

public class ExpenseCategoryMonthlyExpensesExceeded : MarkupValueConverter<ExpenseCategoryMonthlyExpensesExceeded>
{
    // ReSharper disable EmptyConstructor
    public ExpenseCategoryMonthlyExpensesExceeded() { }
    // ReSharper restore EmptyConstructor

    public override object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        var vm = value as ExpenseCategoryViewModelEx;
        if (vm != null)
        {
            if (vm.BudgetedPercentage > 0)
                return DependencyProperty.UnsetValue;
            return vm.MonthlyExpenses > vm.BudgetedAmount;
        }
        return DependencyProperty.UnsetValue;
    }
}