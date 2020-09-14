using System;
using System.Globalization;
using System.Windows;

namespace SimplyBudget.ValueConverter
{
    public class PercentValueConverter : MarkupMultiValueConverter<PercentValueConverter>
    {
        // ReSharper disable once EmptyConstructor
        public PercentValueConverter() { }

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int percentage = values[0] != null && values[0] != DependencyProperty.UnsetValue ? System.Convert.ToInt32(values[0]) : 0;
            int total = System.Convert.ToInt32(values[1]);

            return System.Convert.ChangeType(total * (percentage / 100.0), targetType);
        }
    }
}