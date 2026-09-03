using Microsoft.Extensions.Caching.Memory;

using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Services;

public class BudgetMonthDataCacheTests
{
    [Test]
    public async Task GetOrCreateAsync_UsesMonthAsPartOfCacheKey()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new BudgetMonthDataCache(memoryCache);

        int factoryCallCount = 0;

        Task<string> CreateValue()
        {
            factoryCallCount++;
            return Task.FromResult($"value-{factoryCallCount}");
        }

        var januaryFirst = await cache.GetOrCreateAsync("budget", new DateTime(2026, 1, 1), "default", CreateValue);
        var januarySecond = await cache.GetOrCreateAsync("budget", new DateTime(2026, 1, 10), "default", CreateValue);
        var february = await cache.GetOrCreateAsync("budget", new DateTime(2026, 2, 1), "default", CreateValue);

        await Assert.That(januaryFirst).IsEqualTo("value-1");
        await Assert.That(januarySecond).IsEqualTo("value-1");
        await Assert.That(february).IsEqualTo("value-2");
    }

    [Test]
    public async Task InvalidateMonth_OnlyRemovesThatMonthCacheEntries()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new BudgetMonthDataCache(memoryCache);

        int januaryFactoryCallCount = 0;
        int februaryFactoryCallCount = 0;

        Task<string> CreateJanuaryValue()
        {
            januaryFactoryCallCount++;
            return Task.FromResult($"jan-{januaryFactoryCallCount}");
        }

        Task<string> CreateFebruaryValue()
        {
            februaryFactoryCallCount++;
            return Task.FromResult($"feb-{februaryFactoryCallCount}");
        }

        _ = await cache.GetOrCreateAsync("history", new DateTime(2026, 1, 1), "default", CreateJanuaryValue);
        _ = await cache.GetOrCreateAsync("history", new DateTime(2026, 2, 1), "default", CreateFebruaryValue);

        cache.InvalidateMonth(new DateTime(2026, 1, 15));

        var januaryAfterInvalidation = await cache.GetOrCreateAsync("history", new DateTime(2026, 1, 20), "default", CreateJanuaryValue);
        var februaryAfterInvalidation = await cache.GetOrCreateAsync("history", new DateTime(2026, 2, 20), "default", CreateFebruaryValue);

        await Assert.That(januaryAfterInvalidation).IsEqualTo("jan-2");
        await Assert.That(februaryAfterInvalidation).IsEqualTo("feb-1");
    }

    [Test]
    public async Task InvalidateMonth_RemovesAllMonthsEntries()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new BudgetMonthDataCache(memoryCache);

        int factoryCallCount = 0;

        Task<string> CreateValue()
        {
            factoryCallCount++;
            return Task.FromResult($"all-{factoryCallCount}");
        }

        _ = await cache.GetOrCreateAsync(
            scope: "pending-expenses",
            month: null,
            cacheVariant: "default",
            valueFactory: CreateValue);

        cache.InvalidateMonth(new DateTime(2026, 1, 1));

        var valueAfterInvalidation = await cache.GetOrCreateAsync(
            scope: "pending-expenses",
            month: null,
            cacheVariant: "default",
            valueFactory: CreateValue);

        await Assert.That(valueAfterInvalidation).IsEqualTo("all-2");
    }

    [Test]
    public async Task InvalidateAllMonths_RemovesEntriesForEveryMonth()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new BudgetMonthDataCache(memoryCache);

        int januaryFactoryCallCount = 0;
        int februaryFactoryCallCount = 0;
        int allMonthsFactoryCallCount = 0;

        Task<string> CreateJanuaryValue()
        {
            januaryFactoryCallCount++;
            return Task.FromResult($"jan-{januaryFactoryCallCount}");
        }

        Task<string> CreateFebruaryValue()
        {
            februaryFactoryCallCount++;
            return Task.FromResult($"feb-{februaryFactoryCallCount}");
        }

        Task<string> CreateAllMonthsValue()
        {
            allMonthsFactoryCallCount++;
            return Task.FromResult($"all-{allMonthsFactoryCallCount}");
        }

        _ = await cache.GetOrCreateAsync("budget", new DateTime(2026, 1, 1), "default", CreateJanuaryValue);
        _ = await cache.GetOrCreateAsync("budget", new DateTime(2026, 2, 1), "default", CreateFebruaryValue);
        _ = await cache.GetOrCreateAsync("pending-expenses", null, "default", CreateAllMonthsValue);

        cache.InvalidateAllMonths();

        var januaryAfterInvalidation = await cache.GetOrCreateAsync("budget", new DateTime(2026, 1, 20), "default", CreateJanuaryValue);
        var februaryAfterInvalidation = await cache.GetOrCreateAsync("budget", new DateTime(2026, 2, 20), "default", CreateFebruaryValue);
        var allMonthsAfterInvalidation = await cache.GetOrCreateAsync("pending-expenses", null, "default", CreateAllMonthsValue);

        await Assert.That(januaryAfterInvalidation).IsEqualTo("jan-2");
        await Assert.That(februaryAfterInvalidation).IsEqualTo("feb-2");
        await Assert.That(allMonthsAfterInvalidation).IsEqualTo("all-2");
    }
}
