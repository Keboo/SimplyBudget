using System;
using System.Collections;
using System.Globalization;

namespace SimplyBudget.ValueConverter
{
    public class HasMultipleValueConverter : MarkupValueConverter<HasMultipleValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public HasMultipleValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                var enumerator = enumerable.GetEnumerator();
                return enumerator.MoveNext() && enumerator.MoveNext();
            }
            return false;
        }
    }
}