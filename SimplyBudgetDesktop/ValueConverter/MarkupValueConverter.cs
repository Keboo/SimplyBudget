using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace SimplyBudget.ValueConverter
{
    [MarkupExtensionReturnType(typeof(IValueConverter))]
    public abstract class MarkupValueConverter<T> : MarkupExtension, IValueConverter where T : IValueConverter, new()
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new T();
        }

        public virtual object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new InvalidOperationException();
        }

        public virtual object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new InvalidOperationException();
        }
    }

    [MarkupExtensionReturnType(typeof(IValueConverter))]
    public abstract class MarkupMultiValueConverter<T> : MarkupExtension, IMultiValueConverter where T : IMultiValueConverter, new()
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new T();
        }

        public virtual object? Convert(object[]? values, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }

        public virtual object[]? ConvertBack(object? value, Type[]? targetTypes, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}