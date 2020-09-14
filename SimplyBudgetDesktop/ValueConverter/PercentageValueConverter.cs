
using System;
using System.Globalization;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ValueConverter
{
    internal class PercentageValueConverter : MarkupValueConverter<PercentageValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public PercentageValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                var intValue = (int) value;
                return intValue.FormatPercentage();
            }
            return null;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                double rv;
                var valueString = value.ToString().Replace(culture.NumberFormat.PercentSymbol, "");
                if (double.TryParse(valueString, NumberStyles.Number, culture, out rv))
                    return (int)rv;
            }
            return double.NaN;
        }
    }
}
