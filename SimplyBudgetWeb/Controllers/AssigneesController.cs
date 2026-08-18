using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

/// <summary>
/// Manages the people that a <see cref="PendingExpense"/> can optionally be assigned to.
/// This is a web-only concept and does not exist in the desktop client.
/// </summary>
[ApiController]
[Route("api/assignees")]
public class AssigneesController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<AssigneeDto[]> GetAll()
    {
        var assignees = await context.PendingExpenseAssignees
            .OrderBy(x => x.Name)
            .ToListAsync();
        return assignees.Select(ToDto).ToArray();
    }

    [HttpPost]
    public async Task<ActionResult<AssigneeDto>> Create([FromBody] AssigneeRequest request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        var existing = await context.PendingExpenseAssignees
            .FirstOrDefaultAsync(x => x.Name == name);
        if (existing is not null)
        {
            return Conflict(ToDto(existing));
        }

        var assignee = new PendingExpenseAssignee { Name = name };
        context.PendingExpenseAssignees.Add(assignee);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { }, ToDto(assignee));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var assignee = await context.PendingExpenseAssignees.FindAsync(id);
        if (assignee is null) return NotFound();

        context.PendingExpenseAssignees.Remove(assignee);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private static AssigneeDto ToDto(PendingExpenseAssignee a) => new(
        Id: a.ID,
        Name: a.Name
    );
}

public record AssigneeDto(int Id, string Name);

public record AssigneeRequest(string? Name);
