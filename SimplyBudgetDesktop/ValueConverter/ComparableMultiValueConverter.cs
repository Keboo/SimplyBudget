using System;
using System.Globalization;
using System.Windows;

namespace SimplyBudget.ValueConverter
{
    public class ComparableMultiValueConverter : MarkupMultiValueConverter<ComparableMultiValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public ComparableMultiValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object? Convert(object[]? values, Type? targetType, object? parameter, CultureInfo? culture)
        {
            if (values is null || values.Length != 2)
                return DependencyProperty.UnsetValue;

            var first = values[0] as IComparable;
            var second = values[1] as IComparable;
            if (first is null || second is null)
                return DependencyProperty.UnsetValue;
            var rv = first.CompareTo(second);
            return Math.Max(-1, Math.Min(1, rv));
        }
    }
}