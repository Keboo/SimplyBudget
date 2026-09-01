using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/rules")]
public class RulesController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<RuleDto[]> GetAll()
    {
        var rules = await context.ExpenseCategoryRules
            .Include(r => r.ExpenseCategory)
            .ToListAsync();
        return rules.Select(ToDto).ToArray();
    }

    [HttpPost]
    public async Task<ActionResult<RuleDto>> Create([FromBody] RuleRequest request)
    {
        if (ValidateAmountRange(request) is { } error)
            return BadRequest(error);

        var rule = new ExpenseCategoryRule
        {
            Name = request.Name,
            RuleRegex = request.RuleRegex,
            Notes = request.Notes,
            MinimumAmount = request.MinimumAmount,
            MaximumAmount = request.MaximumAmount,
            ExpenseCategoryID = request.ExpenseCategoryId,
        };
        context.ExpenseCategoryRules.Add(rule);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { }, ToDto(rule));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RuleDto>> Update(int id, [FromBody] RuleRequest request)
    {
        var rule = await context.ExpenseCategoryRules
            .Include(r => r.ExpenseCategory)
            .FirstOrDefaultAsync(r => r.ID == id);
        if (rule is null) return NotFound();

        if (ValidateAmountRange(request) is { } error)
            return BadRequest(error);

        rule.Name = request.Name;
        rule.RuleRegex = request.RuleRegex;
        rule.Notes = request.Notes;
        rule.MinimumAmount = request.MinimumAmount;
        rule.MaximumAmount = request.MaximumAmount;
        rule.ExpenseCategoryID = request.ExpenseCategoryId;

        await context.SaveChangesAsync();
        return ToDto(rule);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await context.ExpenseCategoryRules.FindAsync(id);
        if (rule is null) return NotFound();

        context.ExpenseCategoryRules.Remove(rule);
        await context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Amount ranges are optional, but when supplied they must be non-negative and
    /// the minimum must not exceed the maximum.
    /// </summary>
    private static string? ValidateAmountRange(RuleRequest request)
    {
        if (request.MinimumAmount < 0 || request.MaximumAmount < 0)
            return "Amount range values cannot be negative.";

        if (request.MinimumAmount is { } min && request.MaximumAmount is { } max && min > max)
            return "Minimum amount cannot be greater than maximum amount.";

        return null;
    }

    private static RuleDto ToDto(ExpenseCategoryRule r) => new(
        Id: r.ID,
        Name: r.Name,
        RuleRegex: r.RuleRegex,
        Notes: r.Notes,
        MinimumAmount: r.MinimumAmount,
        MaximumAmount: r.MaximumAmount,
        ExpenseCategoryId: r.ExpenseCategoryID,
        CategoryName: r.ExpenseCategory?.Name
    );
}

public record RuleDto(
    int Id,
    string? Name,
    string? RuleRegex,
    string? Notes,
    int? MinimumAmount,
    int? MaximumAmount,
    int? ExpenseCategoryId,
    string? CategoryName
);

public record RuleRequest(
    string? Name,
    string? RuleRegex,
    int? ExpenseCategoryId,
    string? Notes = null,
    int? MinimumAmount = null,
    int? MaximumAmount = null);
