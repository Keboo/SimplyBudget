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

                    query = query.Where(x =>
                        (x.Description != null && x.Description.Contains(searchText)) ||
                        (x.Notes != null && x.Notes.Contains(searchText)) ||
                        x.Details!.Any(d => d.ExpenseCategory!.Description != null && d.ExpenseCategory.Description.Contains(searchText)) ||
                        (hasSearchAmount &&
                         (x.Details!.Any(d => Math.Abs(d.Amount) == searchAmountAbs) ||
                          Math.Abs(x.Details!.Sum(d => d.Amount)) == searchAmountAbs)));
                }

                if (categoryId.HasValue)
                    query = query.Where(x => x.Details!.Any(d => d.ExpenseCategoryId == categoryId.Value));

                if (accountId.HasValue)
                    query = query.Where(x => x.Details!.Any(d => d.ExpenseCategory!.AccountID == accountId.Value));

                var items = await query
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.ID)
                    .ToListAsync();

                return items.Select(ToDto).ToArray();
            });
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
        return ToDto(item);
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

    private static HistoryItemDto ToDto(ExpenseCategoryItem item) => new(
        Id: item.ID,
        Date: item.Date,
        Description: item.Description,
        Notes: item.Notes,
        IsTransfer: item.IsTransfer,
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
