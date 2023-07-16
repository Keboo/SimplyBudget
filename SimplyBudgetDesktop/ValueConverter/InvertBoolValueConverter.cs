namespace SimplyBudget.ValueConverter;

public class InvertBoolValueConverter : MarkupValueConverter<InvertBoolValueConverter>
{
    // ReSharper disable EmptyConstructor
    public InvertBoolValueConverter() { }
    // ReSharper restore EmptyConstructor

    public override object? Convert(object? value, System.Type? targetType, object? parameter, System.Globalization.CultureInfo? culture)
    {
        if (value is bool)
            return ((bool) value) == false;
        return null;
    }

    public override object? ConvertBack(object? value, System.Type? targetType, object? parameter, System.Globalization.CultureInfo? culture)
    {
        if (value is bool)
            return ((bool) value) == false;
        return null;
    }
}