using System;
using System.Globalization;
using System.Windows;

namespace SimplyBudget.ValueConverter
{
    public class IsNotTodayValueConverter : MarkupValueConverter<IsNotTodayValueConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime)
            {
                return ((DateTime) value).Date != DateTime.Today;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}