using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Middleware;

/// <summary>
/// Runs after authentication/authorization for every request and upserts a
/// <see cref="Data.PendingExpenseAssignee"/> row for the current user, so the Pending Expenses
/// assignee list always reflects everyone who has signed in.
/// </summary>
public class CurrentUserSyncMiddleware(RequestDelegate next, ILogger<CurrentUserSyncMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, CurrentUserSyncService syncService, BudgetWebContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                await syncService.SyncAsync(context.User, context.RequestAborted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log but don't fail the request — sync is a best-effort background operation.
                // Clear any pending tracked changes so the failed sync does not corrupt the
                // shared DbContext for the rest of the request pipeline.
                logger.LogError(ex, "Failed to sync current user assignee record.");
                dbContext.ChangeTracker.Clear();
            }
        }

        await next(context);
    }
}
