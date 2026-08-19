using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

/// <summary>
/// Manages pending expenses: receipts/transactions that have occurred but have not yet been
/// categorized (or split) into a real expense item. This is a web-only concept and does not
/// exist in the desktop client.
/// </summary>
[ApiController]
[Route("api/pending-expenses")]
public class PendingExpensesController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<PendingExpenseDto[]> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? assigneeId)
    {
        var query = context.PendingExpenses
            .Include(x => x.Assignee)
            .Include(x => x.SuggestedCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Description != null && x.Description.Contains(search));

        if (assigneeId.HasValue)
            query = query.Where(x => x.AssigneeId == assigneeId.Value);

        // Oldest first: pending expenses are worked off like a queue, not limited to a month.
        var items = await query
            .OrderBy(x => x.Date)
            .ThenBy(x => x.ID)
            .ToListAsync();

        return items.Select(ToDto).ToArray();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PendingExpenseDto>> GetById(int id)
    {
        var pending = await context.PendingExpenses
            .Include(x => x.Assignee)
            .Include(x => x.SuggestedCategory)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (pending is null) return NotFound();

        return ToDto(pending);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PendingExpenseDto>> Update(int id, [FromBody] PendingExpenseUpdateRequest request)
    {
        // AsTracking() guarantees this entity is change-tracked (and its mutations below actually
        // saved) even if the context's default query tracking behavior has been set to NoTracking.
        var pending = await context.PendingExpenses
            .AsTracking()
            .Include(x => x.Assignee)
            .Include(x => x.SuggestedCategory)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (pending is null) return NotFound();

        if (request.AssigneeId.HasValue &&
            !await context.PendingExpenseAssignees.AnyAsync(x => x.ID == request.AssigneeId.Value))
        {
            return BadRequest($"Assignee {request.AssigneeId} not found.");
        }

        pending.AssigneeId = request.AssigneeId;
        pending.Notes = request.Notes;
        await context.SaveChangesAsync();

        // AssigneeId may have changed since the initial .Include(x => x.Assignee) load, and
        // Reference(...).LoadAsync() is normally a no-op once a navigation is marked as loaded.
        // Reset the loaded flag so it re-queries using the (possibly new/cleared) AssigneeId.
        context.Entry(pending).Reference(x => x.Assignee).IsLoaded = false;
        await context.Entry(pending).Reference(x => x.Assignee).LoadAsync();

        return ToDto(pending);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pending = await context.PendingExpenses.FindAsync(id);
        if (pending is null) return NotFound();

        context.PendingExpenses.Remove(pending);
        await context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Deletes every pending expense matching the given filters (the same filters used by
    /// <see cref="GetAll"/>), so "delete all" only discards what is currently visible to the user.
    /// Omitting both filters deletes every pending expense.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAll(
        [FromQuery] string? search,
        [FromQuery] int? assigneeId)
    {
        var query = context.PendingExpenses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Description != null && x.Description.Contains(search));

        if (assigneeId.HasValue)
            query = query.Where(x => x.AssigneeId == assigneeId.Value);

        context.PendingExpenses.RemoveRange(query);
        await context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Converts a pending expense into a real expense item (or income item, if it represents a
    /// credit), optionally split across multiple categories, and removes it from the pending list.
    /// </summary>
    [HttpPost("{id}/convert")]
    public async Task<IActionResult> Convert(int id, [FromBody] ConvertPendingExpenseRequest request)
    {
        var pending = await context.PendingExpenses.FindAsync(id);
        if (pending is null) return NotFound();

        if (request.Items is null || request.Items.Length == 0)
            return BadRequest("At least one category item is required.");

        var items = request.Items.Select(i => (i.Amount, i.ExpenseCategoryId)).ToArray();

        if (pending.IsDebit)
        {
            await context.AddTransaction(request.Description, request.Date, items);
        }
        else
        {
            await context.AddIncome(request.Description, request.Date, items);
        }

        context.PendingExpenses.Remove(pending);
        await context.SaveChangesAsync();

        return StatusCode(201);
    }

    private static PendingExpenseDto ToDto(PendingExpense p) => new(
        Id: p.ID,
        Date: p.Date,
        Description: p.Description,
        Amount: p.Amount,
        IsDebit: p.IsDebit,
        Notes: p.Notes,
        AssigneeId: p.AssigneeId,
        AssigneeName: p.Assignee?.Name,
        SuggestedCategoryId: p.SuggestedCategoryId,
        SuggestedCategoryName: p.SuggestedCategory?.Name
    );
}

public record PendingExpenseDto(
    int Id,
    DateTime Date,
    string? Description,
    int Amount,
    bool IsDebit,
    string? Notes,
    int? AssigneeId,
    string? AssigneeName,
    int? SuggestedCategoryId,
    string? SuggestedCategoryName
);

public record PendingExpenseUpdateRequest(int? AssigneeId, string? Notes);

public record ConvertPendingExpenseItemRequest(int ExpenseCategoryId, int Amount);

public record ConvertPendingExpenseRequest(string Description, DateTime Date, ConvertPendingExpenseItemRequest[] Items);
