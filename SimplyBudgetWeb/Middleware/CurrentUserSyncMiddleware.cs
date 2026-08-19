using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Middleware;

/// <summary>
/// Runs after authentication/authorization for every request and upserts a
/// <see cref="Data.PendingExpenseAssignee"/> row for the current user, so the Pending Expenses
/// assignee list always reflects everyone who has signed in.
/// </summary>
public class CurrentUserSyncMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CurrentUserSyncService syncService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await syncService.SyncAsync(context.User, context.RequestAborted);
        }

        await next(context);
    }
}
