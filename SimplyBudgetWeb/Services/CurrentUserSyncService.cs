using System.Security.Claims;

using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Services;

/// <summary>
/// Ensures a <see cref="PendingExpenseAssignee"/> row exists (and stays up to date) for every
/// signed-in user. There is no manual "add assignee" flow: the Pending Expenses assignee list
/// is simply the set of people who have logged in, keyed by their Entra ID object ID.
/// </summary>
public class CurrentUserSyncService(BudgetWebContext context)
{
    public async Task SyncAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var objectId = user.GetObjectId();
        if (string.IsNullOrEmpty(objectId))
        {
            // Not an authenticated Entra ID user (or the oid claim is missing) - nothing to sync.
            return;
        }

        // Prefer the actual "name" claim for display purposes - GetDisplayName() falls back to
        // preferred_username (the UPN/email) first, which reads poorly in the assignee dropdown.
        var name = user.FindFirstValue("name")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.GetDisplayName()
            ?? objectId;
        var email = user.FindFirstValue(ClaimTypes.Upn)
            ?? user.FindFirstValue("preferred_username")
            ?? user.FindFirstValue(ClaimTypes.Email);

        // AsTracking() guarantees this entity is change-tracked (and its mutations below actually
        // saved) even if the context's default query tracking behavior has been set to NoTracking.
        var assignee = await context.PendingExpenseAssignees
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ObjectId == objectId, cancellationToken);

        if (assignee is null)
        {
            context.PendingExpenseAssignees.Add(new PendingExpenseAssignee
            {
                ObjectId = objectId,
                Name = name,
                Email = email,
                LastLoginUtc = DateTime.UtcNow,
            });
        }
        else if (assignee.Name != name || assignee.Email != email)
        {
            assignee.Name = name;
            assignee.Email = email;
            assignee.LastLoginUtc = DateTime.UtcNow;
        }
        else
        {
            assignee.LastLoginUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
