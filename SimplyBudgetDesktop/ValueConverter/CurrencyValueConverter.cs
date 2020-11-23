
using System;
using System.Globalization;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ValueConverter
{
    internal class CurrencyValueConverter : MarkupValueConverter<CurrencyValueConverter>
    {
        // ReSharper disable EmptyConstructor
        public CurrencyValueConverter() { }
        // ReSharper restore EmptyConstructor

        public override object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            if (value is int)
            {
                var intValue = (int) value;
                return intValue.FormatCurrencyOld();
            }
#if DEBUG
            if (value != null)
            {
                System.Diagnostics.Debugger.Break();
            }
#else 
            if (value is double) //TODO: Delete this after finding all of the bugs
            {
                var intVal = (int) (double) value;
                return intVal.FormatCurrencyOld();
            }
#endif
            return null;
        }

        public override object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            if (value != null)
            {
                var stringValue = value.ToString();
                if (string.IsNullOrWhiteSpace(stringValue)) return 0;

                decimal rv;
                if (decimal.TryParse(stringValue, NumberStyles.Currency, culture, out rv))
                {
                    return (int)decimal.Round(rv * 100);
                }
            }
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
            return null;
        }
    }
}
