using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Data;

using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

/// <summary>
/// Exposes the people that a <see cref="PendingExpense"/> can optionally be assigned to.
/// This is a web-only concept and does not exist in the desktop client. There is no manual
/// "add assignee" flow - rows are created and kept up to date automatically whenever a user
/// signs in (see <see cref="Services.CurrentUserSyncService"/>), so this list is simply
/// everyone who has logged in.
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
