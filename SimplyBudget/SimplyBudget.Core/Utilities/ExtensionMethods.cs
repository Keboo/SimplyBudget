using SimplyBudget.Core.Data;
using System.Globalization;

namespace SimplyBudget.Core.Utilities;

public static class ExtensionMethods
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? toAdd)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (toAdd != null)
        {
            foreach (var item in toAdd)
                collection.Add(item);
        }
    }

    public static void RemoveFirst<T>(this IList<T> collection, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);

        for (int i = 0; i < collection.Count; i++)
        {
            if (predicate(collection[i]))
            {
                collection.RemoveAt(i);
                break;
            }
        }
    }

    public static string FormatCurrency(this int amount)
    {
        //Negative values are transactions, positive values are income
        return $"{amount/100.0:c}";
    }

    public static string GetDisplayAmount(this ExpenseCategoryItem expenseItem)
    {
        ArgumentNullException.ThrowIfNull(expenseItem);

        int total = expenseItem.Details?.Sum(x => x.Amount) ?? 0;
        if (expenseItem.Details?.Count == 2 && total == 0)
        {
            return $"<{Math.Abs(expenseItem.Details[0].Amount).FormatCurrency()}>";
        }
        return total.FormatCurrency();

    }

    public static string FormatPercentage(this int percentage)
    {
        return string.Concat(percentage, CultureInfo.CurrentUICulture.NumberFormat.PercentSymbol);
    }

    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(-1 * (dateTime.Day - 1));
    }

    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, DateTime.DaysInMonth(dateTime.Year, dateTime.Month));
    }
}