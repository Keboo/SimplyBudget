using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimplyBudget.ValueConverter;

public class InverseBoolToVisConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return booleanValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return DependencyProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return DependencyProperty.UnsetValue;
    }
}
