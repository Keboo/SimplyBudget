using System.Globalization;

namespace SimplyBudgetWeb.Utilities;

internal static class SearchAmountParser
{
    public static bool TryParseAmountInCents(string? value, out int amountInCents)
    {
        amountInCents = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();
        if (!decimal.TryParse(trimmedValue, NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount) &&
            !decimal.TryParse(trimmedValue, NumberStyles.Currency, CultureInfo.InvariantCulture, out amount))
        {
            return false;
        }

        amountInCents = (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        return true;
    }
}
