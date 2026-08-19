using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Import;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Data;
using System.Text.RegularExpressions;

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

        var rules = await context.ExpenseCategoryRules
            .Include(x => x.ExpenseCategory)
            .ToListAsync();

        var result = new List<ImportItemDto>();
        foreach (var item in parsedItems)
        {
            var rawAmount = item.Details?.FirstOrDefault()?.Amount ?? 0;
            var rule = rules.FirstOrDefault(r => r.RuleRegex != null &&
                Regex.IsMatch(item.Description ?? "", r.RuleRegex, RegexOptions.IgnoreCase));

            result.Add(new ImportItemDto(
                Date: item.Date,
                Description: item.Description,
                Amount: Math.Abs(rawAmount),
                IsDebit: rawAmount < 0,
                SuggestedCategoryId: rule?.ExpenseCategoryID,
                SuggestedCategoryName: rule?.ExpenseCategory?.Name,
                IsDone: false
            ));
        }

        return Ok(result.ToArray());
    }

    /// <summary>
    /// Saves parsed import items as pending expenses so they can be reviewed, assigned, and
    /// categorized/split into real expense items later from the Pending Expenses page. Items the
    /// user has checked off as already handled (<see cref="ImportItemDto.IsDone"/>) are skipped.
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] ImportItemDto[] items)
    {
        var pendingExpenses = items
            .Where(i => !i.IsDone)
            .Select(i => new PendingExpense
            {
                Date = i.Date,
                Description = i.Description,
                Amount = i.Amount,
                IsDebit = i.IsDebit,
                SuggestedCategoryId = i.SuggestedCategoryId,
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
    int? SuggestedCategoryId,
    string? SuggestedCategoryName,
    bool IsDone
);
