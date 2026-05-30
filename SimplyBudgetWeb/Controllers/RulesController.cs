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
        var rule = new ExpenseCategoryRule
        {
            Name = request.Name,
            RuleRegex = request.RuleRegex,
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

        rule.Name = request.Name;
        rule.RuleRegex = request.RuleRegex;
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

    private static RuleDto ToDto(ExpenseCategoryRule r) => new(
        Id: r.ID,
        Name: r.Name,
        RuleRegex: r.RuleRegex,
        ExpenseCategoryId: r.ExpenseCategoryID,
        CategoryName: r.ExpenseCategory?.Name
    );
}

public record RuleDto(
    int Id,
    string? Name,
    string? RuleRegex,
    int? ExpenseCategoryId,
    string? CategoryName
);

public record RuleRequest(string? Name, string? RuleRegex, int? ExpenseCategoryId);
