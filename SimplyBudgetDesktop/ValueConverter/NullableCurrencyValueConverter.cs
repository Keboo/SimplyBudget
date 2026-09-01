using SimplyBudgetShared.Utilities;
using System;
using System.Globalization;

namespace SimplyBudget.ValueConverter;

/// <summary>
/// Converts an optional amount (in cents) to/from a currency string, treating an empty
/// string as "no value" rather than zero.
/// </summary>
public class NullableCurrencyValueConverter : MarkupValueConverter<NullableCurrencyValueConverter>
{
    // ReSharper disable EmptyConstructor
    public NullableCurrencyValueConverter() { }
    // ReSharper restore EmptyConstructor

    public override object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => value is int intValue ? intValue.FormatCurrency() : "";

    public override object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        var stringValue = value?.ToString();
        if (string.IsNullOrWhiteSpace(stringValue))
            return null;

        if (decimal.TryParse(stringValue, NumberStyles.Currency, culture, out var parsed))
            return (int)decimal.Round(parsed * 100);

        return null;
    }
}
