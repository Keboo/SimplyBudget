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
        if (expenseCategory.Cap is int cap)
        {
            return Math.Min(remaining, Math.Max(0, cap - expenseCategory.CurrentBalance));
        }
        return remaining;
    }
}

public interface IAccountDataClient : IDataClient<Account>
{ }

public interface IDataClient<T> where T : class
{
    IAsyncEnumerable<T> GetAllAsync();
    IDatasyncQueryable<T> Query();
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
    }

}

