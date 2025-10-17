using System.Linq.Expressions;

using CommunityToolkit.Datasync.Client;

using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Utilities;

namespace SimplyBudgetShared.Data;

public static class DataSyncExtensions
{
    public static async Task<T?> SingleOrDefaultAsync<T>(this IDatasyncQueryable<T> source, Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        if (predicate is not null)
        {
            source = source.Where(predicate);
        }
        await using var enumerator = source.ToAsyncEnumerable().GetAsyncEnumerator();
        if (!await enumerator.MoveNextAsync())
        {
            return default;
        }
        T rv = enumerator.Current;
        return await enumerator.MoveNextAsync() ? throw new InvalidOperationException("Sequence contained multiple elements") : rv;
    }

    public static async Task<T?> FirstOrDefaultAsync<T>(this IDatasyncQueryable<T> source, Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        if (predicate is not null)
        {
            source = source.Where(predicate);
        }
        await using var enumerator = source.ToAsyncEnumerable().GetAsyncEnumerator();
        return !await enumerator.MoveNextAsync() ? default : enumerator.Current;
    }

    public static async Task<bool> AnyAsync<T>(this IDatasyncQueryable<T> source, Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        if (predicate is not null)
        {
            source = source.Where(predicate);
        }
        return await source.CountAsync() > 0;
    }
}

