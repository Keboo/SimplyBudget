using System;
using System.Globalization;

namespace SimplyBudget.ValueConverter
{
    public class NotEqualValueConverter : MarkupValueConverter<NotEqualValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public NotEqualValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            if (value != null && parameter != null)
            {
                if (value.GetType() != parameter.GetType())
                    parameter = System.Convert.ChangeType(parameter, value.GetType());
            }
            return Equals(value, parameter) == false;
        }


    }
}