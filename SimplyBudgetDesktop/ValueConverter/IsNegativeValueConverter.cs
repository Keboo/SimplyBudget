using System;
using System.Globalization;
using System.Windows;

namespace SimplyBudget.ValueConverter
{
    public class IsNegativeValueConverter : MarkupValueConverter<IsNegativeValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public IsNegativeValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
                return ((double) value) < 0.0;
            if (value is short)
                return ((short) value) < 0;
            if (value is int)
                return ((int) value) < 0;
            if (value is long)
                return ((long) value) < 0;
            return DependencyProperty.UnsetValue;
        }
    }
}