using System;
using System.Globalization;

namespace SimplyBudget.ValueConverter
{
    public class ShortDateValueConverter : MarkupValueConverter<ShortDateValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public ShortDateValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime)
            {
                return ((DateTime) value).ToShortDateString();
            }
            return null;
        }
    }
}