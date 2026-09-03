using System.Collections.Concurrent;
using System.Globalization;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

using SimplyBudgetShared.Utilities;

namespace SimplyBudgetWeb.Services;

public interface IBudgetMonthDataCache
{
    Task<T> GetOrCreateAsync<T>(
        string scope,
        DateTime? month,
        string cacheVariant,
        Func<Task<T>> valueFactory,
        CancellationToken cancellationToken = default);

    void InvalidateMonth(DateTime month);
    void InvalidateMonths(IEnumerable<DateTime> months);
    void InvalidateAllMonths();
}

public sealed class BudgetMonthDataCache(IMemoryCache cache) : IBudgetMonthDataCache
{
    private const string AllMonthsKey = "all-months";
    private static readonly TimeSpan CacheEntryLifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> monthTokens = new(StringComparer.Ordinal);

    public Task<T> GetOrCreateAsync<T>(
        string scope,
        DateTime? month,
        string cacheVariant,
        Func<Task<T>> valueFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var monthKey = ToMonthKey(month);
        var normalizedVariant = string.IsNullOrWhiteSpace(cacheVariant)
            ? "default"
            : cacheVariant.Trim();
        var cacheKey = $"screen:{scope}:month:{monthKey}:variant:{normalizedVariant}";

        return cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheEntryLifetime;
            entry.AddExpirationToken(new CancellationChangeToken(GetToken(monthKey).Token));

            cancellationToken.ThrowIfCancellationRequested();
            return await valueFactory();
        })!;
    }

    public void InvalidateMonth(DateTime month)
    {
        InvalidateMonthKey(ToMonthKey(month.StartOfMonth()));
        InvalidateMonthKey(AllMonthsKey);
    }

    public void InvalidateMonths(IEnumerable<DateTime> months)
    {
        ArgumentNullException.ThrowIfNull(months);

        var monthKeys = months
            .Select(x => ToMonthKey(x.StartOfMonth()))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (monthKeys.Length == 0)
            return;

        foreach (var monthKey in monthKeys)
            InvalidateMonthKey(monthKey);

        InvalidateMonthKey(AllMonthsKey);
    }

    public void InvalidateAllMonths()
    {
        foreach (var monthKey in monthTokens.Keys)
            InvalidateMonthKey(monthKey);
    }

    private CancellationTokenSource GetToken(string monthKey)
        => monthTokens.GetOrAdd(monthKey, _ => new CancellationTokenSource());

    private void InvalidateMonthKey(string monthKey)
    {
        if (!monthTokens.TryRemove(monthKey, out var tokenSource))
            return;

        tokenSource.Cancel();
        tokenSource.Dispose();
    }

    private static string ToMonthKey(DateTime? month)
        => month.HasValue ? ToMonthKey(month.Value) : AllMonthsKey;

    private static string ToMonthKey(DateTime month)
        => month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}

public sealed class NullBudgetMonthDataCache : IBudgetMonthDataCache
{
    public static readonly NullBudgetMonthDataCache Instance = new();

    private NullBudgetMonthDataCache()
    {
    }

    public async Task<T> GetOrCreateAsync<T>(
        string scope,
        DateTime? month,
        string cacheVariant,
        Func<Task<T>> valueFactory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await valueFactory();
    }

    public void InvalidateMonth(DateTime month)
    {
    }

    public void InvalidateMonths(IEnumerable<DateTime> months)
    {
    }

    public void InvalidateAllMonths()
    {
    }
}
