using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/current-user")]
public class CurrentUserController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CurrentUserDto>> Get()
    {
        var assignee = await GetCurrentAssigneeAsync();
        if (assignee is null)
        {
            return NotFound("Current user could not be resolved.");
        }

        return ToDto(assignee);
    }

    [HttpPut("display-name")]
    public async Task<ActionResult<CurrentUserDto>> UpdateDisplayName([FromBody] CurrentUserDisplayNameRequest request)
    {
        var assignee = await GetCurrentAssigneeAsync();
        if (assignee is null)
        {
            return NotFound("Current user could not be resolved.");
        }

        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return BadRequest("Display name is required.");
        }

        if (displayName.Length > 100)
        {
            return BadRequest("Display name must be 100 characters or fewer.");
        }

        assignee.Name = displayName;
        assignee.IsNameCustomized = true;
        await context.SaveChangesAsync();

        return ToDto(assignee);
    }

    private async Task<PendingExpenseAssignee?> GetCurrentAssigneeAsync()
    {
        var objectId = User.GetObjectId();
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return null;
        }

        return await context.PendingExpenseAssignees
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ObjectId == objectId);
    }

    private static CurrentUserDto ToDto(PendingExpenseAssignee assignee) => new(
        DisplayName: assignee.Name,
        Email: assignee.Email
    );
}

public record CurrentUserDto(string DisplayName, string? Email);

public record CurrentUserDisplayNameRequest(string? DisplayName);
