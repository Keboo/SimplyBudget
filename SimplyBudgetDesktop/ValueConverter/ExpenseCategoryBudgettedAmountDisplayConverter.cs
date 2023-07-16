using SimplyBudgetShared.Data;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SimplyBudget.ValueConverter;

public class ExpenseCategoryBudgettedDisplayStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ExpenseCategory category)
        {
            return category.GetBudgetedDisplayString();
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
