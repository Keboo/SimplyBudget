using Microsoft.EntityFrameworkCore;
using SimplyBudget.Core.Utilities;

namespace SimplyBudget.Core.Data;

public static class BudgetContextExtensions
{
    public static async Task<Account> SetAsDefaultAsync(this BudgetContext context, Account account)
    {
        ArgumentNullException.ThrowIfNull(context);

        ArgumentNullException.ThrowIfNull(account);

        if (await context.Accounts.SingleOrDefaultAsync(x => x.IsDefault) is { } previousDefault)
        {
            previousDefault.IsDefault = false;
        }
        if (await context.Accounts.SingleOrDefaultAsync(x => x.ID == account.ID) is { } newDefault)
        {
            newDefault.IsDefault = true;
            return newDefault;
        }
        account.IsDefault = true;
        context.Accounts.Add(account);
        return account;
    }

    public static async Task<Account?> GetDefaultAccountAsync(this BudgetContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.Accounts.FirstOrDefaultAsync(x => x.IsDefault) ??
               await context.Accounts.FirstOrDefaultAsync();
    }

    public static async Task<int> GetCurrentAmount(this BudgetContext context, int accountId)
    {
        ArgumentNullException.ThrowIfNull(context);

        Account? account = await context.Accounts
            .Include(x => x.ExpenseCategories)
            .FirstOrDefaultAsync(x => x.ID == accountId);

        return account?.ExpenseCategories?.Sum(x => x.CurrentBalance) ?? 0;
    }

    public static async Task<ExpenseCategoryItem> AddTransfer(this BudgetContext context,
        string description,
        DateTime date,
        int amount,
        ExpenseCategory fromCategory, ExpenseCategory toCategory)
    {
        return await context.AddTransfer(description, date, false, amount, fromCategory, toCategory);
    }

    public static async Task<ExpenseCategoryItem> AddTransfer(this BudgetContext context,
        string description,
        DateTime date,
        bool ignoreBudget,
        int amount,
        ExpenseCategory fromCategory,
        ExpenseCategory toCategory)
    {
        var item = new ExpenseCategoryItem
        {
            Date = date.Date,
            Description = description,
            Details =
            [
                new ExpenseCategoryItemDetail
                {
                    Amount = -amount,
                    ExpenseCategoryId = fromCategory.ID,
                },
                new ExpenseCategoryItemDetail
                {
                    Amount = amount,
                    ExpenseCategoryId = toCategory.ID
                }
            ],
            IgnoreBudget = ignoreBudget //NB: Order matters here, must be after Details
        };
        context.ExpenseCategoryItems.Add(item);

        await context.SaveChangesAsync();

        return item;
    }

    public static async Task<ExpenseCategoryItem> AddIncome(this BudgetContext context,
        string description, DateTime date, bool ignoreBudget,
        params (int Amount, int ExpenseCategory)[] items)
    {
        ArgumentNullException.ThrowIfNull(context);

        var item = new ExpenseCategoryItem
        {
            Date = date.Date,
            Description = description,
            Details = items.Select(x => new ExpenseCategoryItemDetail
            {
                Amount = x.Amount,
                ExpenseCategoryId = x.ExpenseCategory,
            }).ToList(),
            IgnoreBudget = ignoreBudget //NB: Order matters here, must be after Details
        };
        context.ExpenseCategoryItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public static async Task<ExpenseCategoryItem> AddIncome(this BudgetContext context,
        string description, DateTime date,
        params (int Amount, int ExpenseCategory)[] items) 
        => await context.AddIncome(description, date, false, items);

    public static async Task<ExpenseCategoryItem> AddTransaction(this BudgetContext context,
        string description, DateTime date, bool ignoreBudget, params (int amount, int expenseCategory)[] items)
    {
        var item = new ExpenseCategoryItem
        {
            Date = date.Date,
            Description = description,
            Details = items.Select(x => new ExpenseCategoryItemDetail
            {
                Amount = -x.amount,
                ExpenseCategoryId = x.expenseCategory,
            }).ToList(),
            IgnoreBudget = ignoreBudget //NB: Order matters here, must be after Details
        };

        context.ExpenseCategoryItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public static async Task<ExpenseCategoryItem> AddTransaction(this BudgetContext context,
        string description, DateTime date, params (int amount, int expenseCategory)[] items)
    {
        return await context.AddTransaction(description, date, false, items);
    }

    //TODO: the order of parameters here doesn't match the others.
    public static async Task<ExpenseCategoryItem> AddTransaction(this BudgetContext context,
        int expenseCategoryId,
        int amount,
        string description,
        DateTime? date = null)
    {
        return await AddTransaction(context, description, date ?? DateTime.Today, false, (amount, expenseCategoryId));
    }

    public static async Task<IList<ExpenseCategoryItem>> GetTransfers(this BudgetContext context, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
    {
        IQueryable<ExpenseCategoryItem> transferQuery = context.ExpenseCategoryItems.Include(x => x.Details)
            .Where(x => x.Details!.Count() == 2 &&
                   x.Details!.Sum(x => x.Amount) == 0 &&
                   x.Details!.Any(x => x.ExpenseCategoryId == expenseCategory.ID));
        if (queryStart != null && queryEnd != null)
        {
            transferQuery = transferQuery.Where(x => x.Date >= queryStart && x.Date <= queryEnd);
        }
        return await transferQuery.ToListAsync();
    }

    public static async Task<IList<ExpenseCategoryItemDetail>> GetCategoryItemDetails(this BudgetContext context, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
    {
        IQueryable<ExpenseCategoryItemDetail> query = context.ExpenseCategoryItemDetails
            .Include(x => x.ExpenseCategoryItem)
            .ThenInclude(x => x!.Details)
            .Where(x => x.ExpenseCategoryId == expenseCategory.ID);

        if (queryStart != null && queryEnd != null)
        {
            query = query
                .Where(x => x.ExpenseCategoryItem!.Date >= queryStart && x.ExpenseCategoryItem.Date <= queryEnd);
        }
        return await query.ToListAsync();
    }

    public static async Task<int> GetRemainingBudgetAmount(this BudgetContext context, ExpenseCategory expenseCategory, DateTime month)
    {
        ArgumentNullException.ThrowIfNull(context);

        ArgumentNullException.ThrowIfNull(expenseCategory);

        if (expenseCategory.UsePercentage) return 0;

        var start = month.StartOfMonth();
        var end = month.EndOfMonth();

        var allocatedAmount =
            await context.ExpenseCategoryItemDetails
                .Include(x => x.ExpenseCategoryItem)
                .Where(x => x.ExpenseCategoryId == expenseCategory.ID)
                .Where(x => x.Amount > 0)
                .Where(x => x.ExpenseCategoryItem!.Date >= start && x.ExpenseCategoryItem.Date <= end)
                .Where(x => x.IgnoreBudget == false)
                .SumAsync(x => x.Amount);
        int remaining = Math.Max(0, expenseCategory.BudgetedAmount - allocatedAmount);
        if (expenseCategory.Cap is int cap)
        {
            return Math.Min(remaining, Math.Max(0, cap - expenseCategory.CurrentBalance));
        }
        return remaining;
    }
}
