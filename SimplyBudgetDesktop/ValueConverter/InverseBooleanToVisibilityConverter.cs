using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SimplyBudget.ValueConverter
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        private static IValueConverter InternalConverter { get; } = new BooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                value = !b;
            }
            return InternalConverter.Convert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                value = v switch
                {
                    Visibility.Collapsed => Visibility.Visible,
                    Visibility.Visible => Visibility.Collapsed,
                    _ => v
                };
            }
            return InternalConverter.Convert(value, targetType, parameter, culture);
        }
    }
}