using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Utilities
{
    public static class ExtensionMethods
    {
        public static Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            return Implementation(items);

            static async Task<List<T>> Implementation(IAsyncEnumerable<T> items)
            {
                var rv = new List<T>();
                await foreach (var item in items)
                {
                    rv.Add(item);
                }
                return rv;
            }
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? toAdd)
        {
            if (collection is null) throw new ArgumentNullException(nameof(collection));
            if (toAdd != null)
            {
                foreach (var item in toAdd)
                    collection.Add(item);
            }
        }

        public static void RemoveFirst<T>(this IList<T> collection, Func<T, bool> predicate)
        {
            if (collection is null) throw new ArgumentNullException(nameof(collection));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    collection.RemoveAt(i);
                    break;
                }
            }
        }

        public static void Raise<T>(this EventHandler<T> handler, object sender, T args) where T : EventArgs
            => handler?.Invoke(sender, args);

        public static string FormatCurrency(this int amount)
        {
            return string.Format("{0:c}", (double)amount / 100);
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
}