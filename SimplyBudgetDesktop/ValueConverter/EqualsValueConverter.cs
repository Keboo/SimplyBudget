using System;
using System.Globalization;

namespace SimplyBudget.ValueConverter
{
    public class EqualsValueConverter : MarkupValueConverter<EqualsValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public EqualsValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter != null)
            {
                if (value.GetType() != parameter.GetType())
                    parameter = System.Convert.ChangeType(parameter, value.GetType());
            }
            return Equals(value, parameter);
        }


    }
}