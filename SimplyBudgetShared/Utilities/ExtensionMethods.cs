using System.Globalization;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace SimplyBudgetShared.Utilities
{
    public static class ExtensionMethods
    {
        public static void AddRange<T>([NotNull] this ICollection<T> collection, [CanBeNull] IEnumerable<T> toAdd)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (toAdd != null)
            {
                foreach (var item in toAdd)
                    collection.Add(item);
            }
        }

        public static void RemoveFirst<T>([NotNull] this IList<T> collection, [NotNull] Func<T, bool> predicate)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (predicate == null) throw new ArgumentNullException("predicate");

            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    collection.RemoveAt(i);
                    break;
                }
            }
        }

        public static void Raise([CanBeNull] this EventHandler handler, object sender, EventArgs e = null)
        {
            if (handler != null)
                handler(sender, e ?? EventArgs.Empty);
        }

        public static void Raise<T>([CanBeNull] this EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            if (handler != null)
                handler(sender, args);
        }

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
            return dateTime.Date.AddDays(-1*(dateTime.Day - 1));
        }

        public static DateTime EndOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, DateTime.DaysInMonth(dateTime.Year, dateTime.Month));
        }
    }
}