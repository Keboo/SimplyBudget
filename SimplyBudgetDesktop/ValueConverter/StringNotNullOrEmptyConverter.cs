
using System;
using System.Globalization;

namespace SimplyBudget.ValueConverter
{
    internal class StringNotNullOrEmptyConverter : MarkupValueConverter<StringNotNullOrEmptyConverter>
    {
        // ReSharper disable EmptyConstructor
        public StringNotNullOrEmptyConverter() { }
        // ReSharper restore EmptyConstructor

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) == false;
        }
    }
}
