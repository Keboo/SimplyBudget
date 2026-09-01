using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;
using SimplyBudgetWeb.Utilities;

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
    private const string ConcurrencyConflictMessage = "This pending expense was changed by another user. Refresh and try again.";

    [HttpGet]
    public async Task<PendingExpenseDto[]> GetAll(
        [FromQuery] DateTime? month = null,
        [FromQuery] string? search = null,
        [FromQuery] int? assigneeId = null)
    {
        var query = context.PendingExpenses
            .Include(x => x.Assignee)
            .Include(x => x.SuggestedCategory)
            .AsQueryable();

        if (month.HasValue)
        {
            var start = month.Value.StartOfMonth();
            var end = start.EndOfMonth();
            query = query.Where(x => x.Date >= start && x.Date <= end);
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = ApplySearchFilter(query, search);

        if (assigneeId.HasValue)
            query = query.Where(x => x.AssigneeId == assigneeId.Value);

        // Oldest first: pending expenses are worked off like a queue.
        var items = await query
            .OrderBy(x => x.Date)
            .ThenBy(x => x.ID)
            .ToListAsync();

        return items.Select(ToDto).ToArray();
    }

    [HttpGet("oldest-month")]
    public async Task<OldestPendingExpenseMonthDto> GetOldestMonth()
    {
        var oldestDate = await context.PendingExpenses
            .OrderBy(x => x.Date)
            .Select(x => (DateTime?)x.Date)
            .FirstOrDefaultAsync();

        DateTime? oldestMonth = oldestDate.HasValue
            ? oldestDate.Value.StartOfMonth()
            : null;

        return new OldestPendingExpenseMonthDto(oldestMonth);
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
        if (!TrySetOriginalVersion(pending, request.Version))
            return BadRequest("Version is required.");

        if (request.AssigneeId.HasValue &&
            !await context.PendingExpenseAssignees.AnyAsync(x => x.ID == request.AssigneeId.Value))
        {
            return BadRequest($"Assignee {request.AssigneeId} not found.");
        }

        pending.AssigneeId = request.AssigneeId;
        pending.Notes = request.Notes;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ConcurrencyConflictMessage);
        }

        // AssigneeId may have changed since the initial .Include(x => x.Assignee) load, so the
        // Assignee navigation may now be stale (pointing at the old assignee, or non-null when
        // it should be cleared). Explicitly refresh it to match the current AssigneeId; setting
        // the navigation directly keeps EF's IsLoaded tracking consistent (unlike forcing
        // IsLoaded = false on an already-loaded, non-null reference, which throws).
        pending.Assignee = request.AssigneeId.HasValue
            ? await context.PendingExpenseAssignees.FindAsync(request.AssigneeId.Value)
            : null;

        return ToDto(pending);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pending = await context.PendingExpenses
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ID == id);
        if (pending is null) return NotFound();
        var requestVersion = Request.Headers.IfMatch.FirstOrDefault();
        if (!TrySetOriginalVersion(pending, requestVersion))
            return BadRequest("If-Match header with version is required.");

        context.PendingExpenses.Remove(pending);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ConcurrencyConflictMessage);
        }
        return NoContent();
    }

    /// <summary>
    /// Deletes every pending expense matching the given filters (the same filters used by
    /// <see cref="GetAll"/>), so "delete all" only discards what is currently visible to the user.
    /// Omitting both filters deletes every pending expense.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAll(
        [FromQuery] DateTime? month = null,
        [FromQuery] string? search = null,
        [FromQuery] int? assigneeId = null)
    {
        var query = context.PendingExpenses.AsQueryable();

        if (month.HasValue)
        {
            var start = month.Value.StartOfMonth();
            var end = start.EndOfMonth();
            query = query.Where(x => x.Date >= start && x.Date <= end);
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = ApplySearchFilter(query, search);

        if (assigneeId.HasValue)
            query = query.Where(x => x.AssigneeId == assigneeId.Value);

        context.PendingExpenses.RemoveRange(query);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("reapply-rules")]
    public async Task<ActionResult<ReapplyPendingExpenseRulesResponse>> ReapplyRules()
    {
        var rules = await context.ExpenseCategoryRules
            .Where(x => x.RuleRegex != null)
            .OrderBy(x => x.ID)
            .ToListAsync();

        var pendingExpenses = await context.PendingExpenses
            .AsTracking()
            .ToListAsync();

        foreach (var pendingExpense in pendingExpenses)
        {
            pendingExpense.SuggestedCategoryId = ExpenseCategoryRuleMatcher.GetSuggestedCategoryId(
                rules,
                pendingExpense.Description,
                isTransaction: pendingExpense.IsDebit);
        }

        await context.SaveChangesAsync();
        return Ok(new ReapplyPendingExpenseRulesResponse(pendingExpenses.Count));
    }

    /// <summary>
    /// Converts a pending expense into a real expense item (or income item, if it represents a
    /// credit), optionally split across multiple categories, and removes it from the pending list.
    /// </summary>
    [HttpPost("{id}/convert")]
    public async Task<IActionResult> Convert(int id, [FromBody] ConvertPendingExpenseRequest request)
    {
        var pending = await context.PendingExpenses
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ID == id);
        if (pending is null) return NotFound();
        if (!TrySetOriginalVersion(pending, request.Version))
            return BadRequest("Version is required.");

        if (request.Items is null || request.Items.Length == 0)
            return BadRequest("At least one category item is required.");

        var items = request.Items.Select(i => (i.Amount, i.ExpenseCategoryId)).ToArray();

        var convertedItemNotes = string.IsNullOrWhiteSpace(request.Notes)
            ? pending.Notes
            : request.Notes;

        ExpenseCategoryItem item;
        if (pending.IsDebit)
        {
            item = await context.AddTransaction(request.Description, request.Date, request.IgnoreBudget, items);
        }
        else
        {
            item = await context.AddIncome(request.Description, request.Date, request.IgnoreBudget, items);
        }
        item.Notes = convertedItemNotes;

        context.PendingExpenses.Remove(pending);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ConcurrencyConflictMessage);
        }

        return StatusCode(201);
    }

    private static PendingExpenseDto ToDto(PendingExpense p) => new(
        Id: p.ID,
        Version: System.Convert.ToBase64String(p.Version),
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

    private bool TrySetOriginalVersion(PendingExpense pending, string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        try
        {
            context.Entry(pending)
                .Property(x => x.Version)
                .OriginalValue = System.Convert.FromBase64String(version.Trim('"'));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static IQueryable<PendingExpense> ApplySearchFilter(IQueryable<PendingExpense> query, string search)
    {
        var searchText = search.Trim();
        var hasSearchAmount = SearchAmountParser.TryParseAmountInCents(searchText, out var searchAmountInCents);
        var searchAmountAbs = Math.Abs(searchAmountInCents);

        return query.Where(x =>
            (x.Description != null && x.Description.Contains(searchText)) ||
            (hasSearchAmount && Math.Abs(x.Amount) == searchAmountAbs));
    }
}

public record PendingExpenseDto(
    int Id,
    string Version,
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

public record OldestPendingExpenseMonthDto(DateTime? Month);

public record PendingExpenseUpdateRequest(int? AssigneeId, string? Notes, string Version);

public record ConvertPendingExpenseItemRequest(int ExpenseCategoryId, int Amount);

public record ConvertPendingExpenseRequest(
    string Description,
    DateTime Date,
    ConvertPendingExpenseItemRequest[] Items,
    string Version,
    bool IgnoreBudget = false,
    string? Notes = null);

public record ReapplyPendingExpenseRulesResponse(int UpdatedCount);
