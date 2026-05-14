using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/history")]
public class HistoryController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<HistoryItemDto[]> GetAll(
        [FromQuery] DateTime? month,
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? accountId)
    {
        var monthDate = (month ?? DateTime.Today).StartOfMonth();
        var start = monthDate.StartOfMonth();
        var end = monthDate.EndOfMonth();

        var query = context.ExpenseCategoryItems
            .Include(x => x.Details!)
                .ThenInclude(d => d.ExpenseCategory)
            .Where(x => x.Date >= start && x.Date <= end)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Description != null && x.Description.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(x => x.Details!.Any(d => d.ExpenseCategoryId == categoryId.Value));

        if (accountId.HasValue)
            query = query.Where(x => x.Details!.Any(d => d.ExpenseCategory!.AccountID == accountId.Value));

        var items = await query.OrderByDescending(x => x.Date).ToListAsync();

        return items.Select(item => new HistoryItemDto(
            Id: item.ID,
            Date: item.Date,
            Description: item.Description,
            IsTransfer: item.IsTransfer,
            Details: (item.Details ?? []).Select(d => new HistoryDetailDto(
                Id: d.ID,
                ExpenseCategoryId: d.ExpenseCategoryId,
                CategoryName: d.ExpenseCategory?.Name,
                Amount: d.Amount,
                IgnoreBudget: d.IgnoreBudget
            )).ToArray()
        )).ToArray();
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
        return NoContent();
    }
}

public record HistoryItemDto(
    int Id,
    DateTime Date,
    string? Description,
    bool IsTransfer,
    HistoryDetailDto[] Details
);

public record HistoryDetailDto(
    int Id,
    int ExpenseCategoryId,
    string? CategoryName,
    int Amount,
    bool IgnoreBudget
);
