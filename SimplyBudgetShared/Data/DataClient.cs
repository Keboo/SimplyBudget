using System.Linq.Expressions;

using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Datasync.Client.Http;

using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Utilities;

namespace SimplyBudgetShared.Data;

public interface IDataClient
{
    IDataClient<Account> Accounts { get; }
    IDataClient<ExpenseCategory> ExpenseCategories { get; }
    IDataClient<ExpenseCategoryItem> ExpenseCategoryItems { get; }
    IDataClient<ExpenseCategoryItemDetail> ExpenseCategoryItemDetails { get; }
    IDataClient<ExpenseCategoryRule> ExpenseCategoryRules { get; }
    IDataClient<Metadata> Metadatas { get; }
}

public static class DataClientExtensions
{
    public static Task<bool> UpdateItemAsync<T>(this IDataClient<T> client, int id, Action<T> updateItem)
        where T : BaseItem
    {
        return client.UpdateItemAsync(x => x.ID == id, updateItem);
    }

    public static async Task<bool> UpdateItemAsync<T>(this IDataClient<T> client, Expression<Func<T, bool>> findItem, Action<T> updateItem)
        where T : class
    {
        var item = await client.Query().SingleOrDefaultAsync(findItem);
        if (item is not null)
        {
            updateItem(item);
            return await client.ReplaceAsync(item) is not null;
        }
        return false;
    }

    public static async Task<Account?> GetDefaultAccountAsync(this IDataClient client)
    {
        return await client.Accounts.Query()
            .OrderByDescending(x => x.IsDefault).FirstOrDefaultAsync();
    }

    public static async Task<int> GetCurrentAmountAsync(this IDataClient client, int accountId, CancellationToken cancellationToken)
    {
        return await client.ExpenseCategories.GetCurrentAmountAsync(accountId, cancellationToken);
    }

    public static async Task<int> GetCurrentAmountAsync(this IDataClient<ExpenseCategory> expenseCategories, int accountId, CancellationToken cancellationToken)
    {
        var categories = await expenseCategories.Query().Where(x => x.AccountID == accountId).ToListAsync(cancellationToken);
        return categories.Sum(x => x.CurrentBalance);
    }

    public static async Task<int> GetRemainingBudgetAmount(this IDataClient dataClient, ExpenseCategory expenseCategory, DateTime month)
    {
        ArgumentNullException.ThrowIfNull(dataClient);
        ArgumentNullException.ThrowIfNull(expenseCategory);

        if (expenseCategory.UsePercentage) return 0;

        var start = month.StartOfMonth();
        var end = month.EndOfMonth();

        var categoryItems = await dataClient.ExpenseCategoryItems.Query()
            .Where(x => x.Date >= start && x.Date <= end)
            .ToListAsync();
        var categoryItemIds = categoryItems.Select(x => x.ID).ToHashSet();

        var details = await dataClient.ExpenseCategoryItemDetails.Query()
            .Where(x => x.ExpenseCategoryId == expenseCategory.ID &&
                        x.Amount > 0 &&
                        categoryItemIds.Contains(x.ExpenseCategoryItemId) &&
                        x.IgnoreBudget == false)
            .ToListAsync();
        var allocatedAmount = details.Sum(x => x.Amount);

        int remaining = Math.Max(0, expenseCategory.BudgetedAmount - allocatedAmount);
        return expenseCategory.Cap is int cap ? Math.Min(remaining, Math.Max(0, cap - expenseCategory.CurrentBalance)) : remaining;
    }

    public static async Task<IList<ExpenseCategoryItemDetail>> GetCategoryItemDetailsAsync(this IDataClient dataClient, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
    {
        List<ExpenseCategoryItemDetail> itemDetails = await dataClient.ExpenseCategoryItemDetails
            .Query()
            .Where(x => x.ExpenseCategoryId == expenseCategory.ID)
            .ToListAsync();

        //.Include(x => x.ExpenseCategoryItem)
        //.ThenInclude(x => x!.Details)
        //.Where(x => x.ExpenseCategoryId == expenseCategory.ID);

        if (queryStart != null && queryEnd != null)
        {
            var detailIds = itemDetails.Select(x => x.ID).ToHashSet();
            var cateogryItems = await dataClient.ExpenseCategoryItems.Query()
                .Where(x => detailIds.Contains(x.ID) && x.Date >= queryStart && x.Date <= queryEnd)
                .ToListAsync();

            itemDetails = [.. itemDetails.Where(x => cateogryItems.Any(ci => ci.ID == x.ExpenseCategoryItemId))];
        }
        return itemDetails;
    }

    public static async ValueTask<ExpenseCategoryItem?> AddTransferAsync(this IDataClient dataClient,
        string description,
        DateTime date,
        int amount,
        ExpenseCategory fromCategory, ExpenseCategory toCategory,
        CancellationToken cancellationToken)
    {
        return await dataClient.AddTransferAsync(description, date, false, amount, fromCategory, toCategory, cancellationToken);

    }

    public static async ValueTask<ExpenseCategoryItem?> AddTransferAsync(this IDataClient dataClient,
        string description,
        DateTime date,
        bool ignoreBudget,
        int amount,
        ExpenseCategory fromCategory,
        ExpenseCategory toCategory,
        CancellationToken cancellationToken)
    {
        var item = new ExpenseCategoryItem
        {
            Date = date.Date,
            Details = [],
            Description = description,
            IgnoreBudget = ignoreBudget //NB: Order matters here, must be after Details
        };

        //TODO: Handle cancellation issues here?
        ExpenseCategoryItem? rv = await dataClient.ExpenseCategoryItems.AddAsync(item, cancellationToken);
        if (rv is not null)
        {
            var from = await dataClient.ExpenseCategoryItemDetails.AddAsync(new()
            {
                Amount = -amount,
                ExpenseCategoryId = fromCategory.ID,
            }, cancellationToken);

            var to = await dataClient.ExpenseCategoryItemDetails.AddAsync(new()
            {
                Amount = amount,
                ExpenseCategoryId = toCategory.ID
            }, cancellationToken);

            if (from is { } && to is { })
            {
                rv.Details = [from, to];
            }
        }

        return rv;
    }

    public static async ValueTask<ExpenseCategoryItem?> AddIncomeAsync(this IDataClient dataClient,
        string description,
        DateTime date,
        bool ignoreBudget,
        (int Amount, int ExpenseCategoryId)[] items,
        CancellationToken cancellationToken)
    {
        var item = new ExpenseCategoryItem
        {
            Date = date.Date,
            Details = [],
            Description = description,
            IgnoreBudget = ignoreBudget //NB: Order matters here, must be after Details
        };

        //TODO: Handle cancellation issues here?
        ExpenseCategoryItem? rv = await dataClient.ExpenseCategoryItems.AddAsync(item, cancellationToken);
        if (rv is not null)
        {
            var details = new List<ExpenseCategoryItemDetail>();
            foreach (var (amount, categoryId) in items)
            {
                var detail = await dataClient.ExpenseCategoryItemDetails.AddAsync(new()
                {
                    Amount = amount,
                    ExpenseCategoryId = categoryId,
                }, cancellationToken);

                if (detail is not null)
                {
                    details.Add(detail);
                }
            }

            if (details.Count > 0)
            {
                rv.Details = details;
            }
        }

        return rv;
    }

    public static async ValueTask<ExpenseCategoryItem?> AddTransactionAsync(this IDataClient dataClient,
        string description,
        DateTime date,
        bool ignoreBudget,
        (int Amount, int ExpenseCategoryId)[] items,
        CancellationToken cancellationToken)
    {
        var item = new ExpenseCategoryItem
        {
            Date = date.Date,
            Details = [],
            Description = description,
            IgnoreBudget = ignoreBudget //NB: Order matters here, must be after Details
        };

        //TODO: Handle cancellation issues here?
        ExpenseCategoryItem? rv = await dataClient.ExpenseCategoryItems.AddAsync(item, cancellationToken);
        if (rv is not null)
        {
            var details = new List<ExpenseCategoryItemDetail>();
            foreach (var (amount, categoryId) in items)
            {
                var detail = await dataClient.ExpenseCategoryItemDetails.AddAsync(new()
                {
                    Amount = -amount,
                    ExpenseCategoryId = categoryId,
                }, cancellationToken);

                if (detail is not null)
                {
                    details.Add(detail);
                }
            }

            if (details.Count > 0)
            {
                rv.Details = details;
            }
        }

        return rv;
    }
}

public interface IDataClient<T> where T : class
{
    IAsyncEnumerable<T> GetAllAsync();
    IDatasyncQueryable<T> Query();
    ValueTask<T?> AddAsync(T item, CancellationToken cancellationToken = default);
    ValueTask<T?> ReplaceAsync(T item, CancellationToken cancellationToken = default);
    ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default);
}

public class DataClient(HttpClientOptions httpClientOptions) : IDataClient
{
    private readonly ItemDataClient<Account> _accountClient = new(httpClientOptions);
    private readonly ItemDataClient<ExpenseCategory> _expenseCategoryClient = new(httpClientOptions);
    private readonly ItemDataClient<ExpenseCategoryItem> _expenseCategoryItemClient = new(httpClientOptions);
    private readonly ItemDataClient<ExpenseCategoryItemDetail> _expenseCategoryItemDetailClient = new(httpClientOptions);
    private readonly ItemDataClient<ExpenseCategoryRule> _expenseCategoryRuleClient = new(httpClientOptions);
    private readonly ItemDataClient<Metadata> _metadataClient = new(httpClientOptions);

    public IDataClient<Account> Accounts => _accountClient;
    public IDataClient<ExpenseCategory> ExpenseCategories => _expenseCategoryClient;
    public IDataClient<ExpenseCategoryItem> ExpenseCategoryItems => _expenseCategoryItemClient;
    public IDataClient<ExpenseCategoryItemDetail> ExpenseCategoryItemDetails => _expenseCategoryItemDetailClient;
    public IDataClient<ExpenseCategoryRule> ExpenseCategoryRules => _expenseCategoryRuleClient;
    public IDataClient<Metadata> Metadatas => _metadataClient;

    private class ItemDataClient<T>(HttpClientOptions httpClientOptions) : IDataClient<T>
        where T : class
    {
        private readonly DatasyncServiceClient<T> _client = new(httpClientOptions);

        public IAsyncEnumerable<T> GetAllAsync() => _client.ToAsyncEnumerable();

        public IDatasyncQueryable<T> Query() => _client.AsQueryable();

        public async ValueTask<T?> AddAsync(T item, CancellationToken cancellationToken)
        {
            var rv = await _client.AddAsync(item, cancellationToken);
            return rv.IsSuccessful && rv.HasValue ? rv.Value : null;
        }

        public async ValueTask<T?> ReplaceAsync(T item, CancellationToken cancellationToken)
        {
            var rv = await _client.ReplaceAsync(item, cancellationToken);
            return rv.IsSuccessful && rv.HasValue ? rv.Value : null;
        }

        public async ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken)
        {
            var rv = await _client.RemoveAsync(item, cancellationToken);
            return rv.IsSuccessful;
        }
    }

}

