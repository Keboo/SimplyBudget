using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Import;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController(BudgetWebContext context) : ControllerBase
{
    [HttpPost("parse")]
    public async Task<ActionResult<ImportItemDto[]>> Parse([FromBody] ImportRequest request)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(request.CsvContent));
        var import = new StcuCsvImport(stream);
        var parsedItems = await import.GetItems().ToListAsync();

        var existingSignedAmountsByDate = await GetExistingSignedAmountsByDateAsync(parsedItems.Select(x => x.Date));

        var result = new List<ImportItemDto>();
        foreach (var item in parsedItems)
        {
            var rawAmount = item.Details?.FirstOrDefault()?.Amount ?? 0;
            

            bool isDuplicate = existingSignedAmountsByDate.TryGetValue(item.Date, out var amounts) &&
                amounts.Contains(rawAmount);

            result.Add(new ImportItemDto(
                Date: item.Date,
                Description: item.Description,
                Amount: Math.Abs(rawAmount),
                IsDebit: rawAmount < 0,
                // Non-duplicate items are checked by default so they're imported; likely
                // duplicates default to unchecked until the user explicitly opts back in.
                IsChecked: !isDuplicate,
                IsDuplicate: isDuplicate
            ));
        }

        return Ok(result.ToArray());
    }

    /// <summary>
    /// Builds a lookup of signed amounts (positive for credits, negative for debits) already
    /// recorded on each date, from both pending expenses and already-categorized expense items.
    /// Used to flag freshly-parsed CSV rows that look like they were already imported.
    /// </summary>
    private async Task<Dictionary<DateTime, HashSet<int>>> GetExistingSignedAmountsByDateAsync(IEnumerable<DateTime> dates)
    {
        var distinctDates = dates.Distinct().ToList();
        var result = new Dictionary<DateTime, HashSet<int>>();
        if (distinctDates.Count == 0) return result;

        var minDate = distinctDates.Min();
        var maxDate = distinctDates.Max();

        void Add(DateTime date, int signedAmount)
        {
            if (!result.TryGetValue(date, out var amounts))
            {
                amounts = new HashSet<int>();
                result[date] = amounts;
            }
            amounts.Add(signedAmount);
        }

        var existingPending = await context.PendingExpenses
            .Where(x => x.Date >= minDate && x.Date <= maxDate)
            .Select(x => new { x.Date, x.Amount, x.IsDebit })
            .ToListAsync();
        foreach (var pe in existingPending)
        {
            Add(pe.Date, pe.IsDebit ? -pe.Amount : pe.Amount);
        }

        var existingExpenseItems = await context.ExpenseCategoryItems
            .Include(x => x.Details)
            .Where(x => x.Date >= minDate && x.Date <= maxDate)
            .ToListAsync();
        foreach (var eci in existingExpenseItems)
        {
            Add(eci.Date, eci.Details?.Sum(d => d.Amount) ?? 0);
        }

        return result;
    }

    /// <summary>
    /// Saves parsed import items as pending expenses so they can be reviewed, assigned, and
    /// categorized/split into real expense items later from the Pending Expenses page. Only
    /// items the user has checked (<see cref="ImportItemDto.IsChecked"/>) are imported.
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] ImportItemDto[] items)
    {
        var rules = await context.ExpenseCategoryRules
            .Where(x => x.RuleRegex != null)
            .OrderBy(x => x.ID)
            .ToListAsync();

        var pendingExpenses = items
            .Where(i => i.IsChecked)
            .Select(i => new PendingExpense
            {
                Date = i.Date,
                Description = i.Description,
                Amount = i.Amount,
                IsDebit = i.IsDebit,
                SuggestedCategoryId = ExpenseCategoryRuleMatcher.GetSuggestedCategoryId(
                    rules,
                    i.Description,
                    isTransaction: i.IsDebit),
            })
            .ToList();

        context.PendingExpenses.AddRange(pendingExpenses);
        await context.SaveChangesAsync();

        return StatusCode(201);
    }
}

public record ImportRequest(string CsvContent);

public record ImportItemDto(
    DateTime Date,
    string? Description,
    int Amount,
    bool IsDebit,
    bool IsChecked,
    bool IsDuplicate = false
);
