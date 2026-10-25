using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;
using SimplyBudgetWeb.Utilities;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/history")]
public class HistoryController(
    BudgetWebContext context,
    IBudgetMonthUpdateNotifier? budgetMonthUpdateNotifier = null,
    IBudgetMonthDataCache? budgetMonthDataCache = null) : ControllerBase
{
    private readonly IBudgetMonthUpdateNotifier budgetMonthUpdates = budgetMonthUpdateNotifier ?? NullBudgetMonthUpdateNotifier.Instance;
    private readonly IBudgetMonthDataCache monthDataCache = budgetMonthDataCache ?? NullBudgetMonthDataCache.Instance;

    [HttpGet]
    public async Task<HistoryItemDto[]> GetAll(
        [FromQuery] DateTime? month,
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? accountId)
    {
        var monthDate = (month ?? DateTime.Today).StartOfMonth();
        var searchText = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var cacheVariant = BuildCacheVariant(searchText, categoryId, accountId);

        return await monthDataCache.GetOrCreateAsync(
            scope: "history",
            month: monthDate,
            cacheVariant: cacheVariant,
            valueFactory: async () =>
            {
                var start = monthDate.StartOfMonth();
                var end = monthDate.EndOfMonth();

                var query = context.ExpenseCategoryItems
                    .Include(x => x.Details!)
                        .ThenInclude(d => d.ExpenseCategory)
                    .Where(x => x.Date >= start && x.Date <= end)
                    .AsQueryable();

                if (searchText is not null)
                {
                    var hasSearchAmount = SearchAmountParser.TryParseAmountInCents(searchText, out var searchAmountInCents);
                    var searchAmountAbs = Math.Abs(searchAmountInCents);
                    var searchAmountDigits = string.Concat(searchText.Where(char.IsDigit));

                    query = query.Where(x =>
                        (x.Description != null && x.Description.Contains(searchText)) ||
                        (x.Notes != null && x.Notes.Contains(searchText)) ||
                        x.Details!.Any(d => d.ExpenseCategory!.Description != null && d.ExpenseCategory.Description.Contains(searchText)) ||
                        (hasSearchAmount && searchAmountDigits.Length > 0 &&
                         (x.Details!.Any(d =>
                              Math.Abs(d.Amount) == searchAmountAbs ||
                              ((Math.Abs(d.Amount) / 100).ToString() +
                               (Math.Abs(d.Amount) % 100 < 10 ? "0" : string.Empty) +
                               (Math.Abs(d.Amount) % 100).ToString()).Contains(searchAmountDigits)) ||
                          Math.Abs(x.Details!.Sum(d => d.Amount)) == searchAmountAbs ||
                          ((Math.Abs(x.Details!.Sum(d => d.Amount)) / 100).ToString() +
                           (Math.Abs(x.Details!.Sum(d => d.Amount)) % 100 < 10 ? "0" : string.Empty) +
                           (Math.Abs(x.Details!.Sum(d => d.Amount)) % 100).ToString()).Contains(searchAmountDigits))));
                }

                if (categoryId.HasValue)
                    query = query.Where(x => x.Details!.Any(d => d.ExpenseCategoryId == categoryId.Value));

                if (accountId.HasValue)
                    query = query.Where(x => x.Details!.Any(d => d.ExpenseCategory!.AccountID == accountId.Value));

                var items = await query
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.ID)
                    .ToListAsync();

                var itemIds = items.Select(x => x.ID).ToArray();
                var receiptsByItemId = await context.ReceiptExpenseLinks
                    .Include(x => x.Receipt!)
                        .ThenInclude(x => x.LineItems)
                    .Where(x => itemIds.Contains(x.ExpenseCategoryItemId))
                    .ToDictionaryAsync(x => x.ExpenseCategoryItemId, x => ReceiptMapper.ToDto(x.Receipt!) with { IsLinked = true });

                return items.Select(item => ToDto(
                    item,
                    receiptsByItemId.GetValueOrDefault(item.ID))).ToArray();
            });
    }

    [HttpGet("{id}/account-balances")]
    public async Task<ActionResult<AccountBalanceSnapshotDto[]>> GetAccountBalancesAtItem(int id)
    {
        var item = await context.ExpenseCategoryItems
            .Where(x => x.ID == id)
            .Select(x => new { x.ID, x.Date })
            .FirstOrDefaultAsync();
        if (item is null) return NotFound();

        var balances = await context.Accounts
            .Select(account => new AccountBalanceSnapshotDto(
                account.ID,
                account.Name,
                account.ExpenseCategories!.Sum(category => (int?)category.CurrentBalance) ?? 0))
            .ToListAsync();

        var laterAmountsByAccount = await context.ExpenseCategoryItemDetails
            .Where(detail => detail.ExpenseCategory!.AccountID != null)
            .Where(detail =>
                detail.ExpenseCategoryItem!.Date > item.Date ||
                (detail.ExpenseCategoryItem.Date == item.Date && detail.ExpenseCategoryItemId > item.ID))
            .GroupBy(detail => detail.ExpenseCategory!.AccountID!.Value)
            .Select(group => new
            {
                AccountId = group.Key,
                Amount = group.Sum(detail => detail.Amount),
            })
            .ToDictionaryAsync(x => x.AccountId, x => x.Amount);

        return balances
            .Select(balance => balance with
            {
                CurrentAmount = balance.CurrentAmount - laterAmountsByAccount.GetValueOrDefault(balance.Id),
            })
            .ToArray();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<HistoryItemDto>> Update(int id, [FromBody] HistoryItemUpdateRequest request)
    {
        var item = await context.ExpenseCategoryItems
            .AsTracking()
            .Include(x => x.Details!)
                .ThenInclude(d => d.ExpenseCategory)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (item is null) return NotFound();

        item.Notes = NormalizeNotes(request.Notes);
        await context.SaveChangesAsync();
        monthDataCache.InvalidateMonth(item.Date);
        var receipt = await context.ReceiptExpenseLinks
            .Include(x => x.Receipt!)
                .ThenInclude(x => x.LineItems)
            .Where(x => x.ExpenseCategoryItemId == item.ID)
            .Select(x => x.Receipt)
            .FirstOrDefaultAsync();
        return ToDto(item, receipt is null ? null : ReceiptMapper.ToDto(receipt) with { IsLinked = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.ExpenseCategoryItems
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (item is null) return NotFound();

        context.ExpenseCategoryItems.Remove(item);
        await context.SaveChangesAsync();
        monthDataCache.InvalidateMonth(item.Date);
        await budgetMonthUpdates.NotifyMonthUpdated(item.Date);
        return NoContent();
    }

    private static string BuildCacheVariant(string? search, int? categoryId, int? accountId)
        => $"search={search ?? string.Empty};categoryId={categoryId?.ToString() ?? string.Empty};accountId={accountId?.ToString() ?? string.Empty}";

    private static HistoryItemDto ToDto(ExpenseCategoryItem item, ReceiptDto? receipt) => new(
        Id: item.ID,
        Date: item.Date,
        Description: item.Description,
        Notes: item.Notes,
        IsTransfer: item.IsTransfer,
        Receipt: receipt,
        Details: (item.Details ?? []).Select(d => new HistoryDetailDto(
            Id: d.ID,
            ExpenseCategoryId: d.ExpenseCategoryId,
            CategoryName: d.ExpenseCategory?.Name,
            CategoryDescription: d.ExpenseCategory?.Description,
            Amount: d.Amount,
            IgnoreBudget: d.IgnoreBudget
        )).ToArray()
    );

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}

public record HistoryItemDto(
    int Id,
    DateTime Date,
    string? Description,
    string? Notes,
    bool IsTransfer,
    ReceiptDto? Receipt,
    HistoryDetailDto[] Details
);

public record HistoryDetailDto(
    int Id,
    int ExpenseCategoryId,
    string? CategoryName,
    string? CategoryDescription,
    int Amount,
    bool IgnoreBudget
);

public record HistoryItemUpdateRequest(string? Notes);

public record AccountBalanceSnapshotDto(
    int Id,
    string? Name,
    int CurrentAmount
);
