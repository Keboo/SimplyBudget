using Microsoft.AspNetCore.SignalR;

using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Hubs;

namespace SimplyBudgetWeb.Services;

public interface IBudgetMonthUpdateNotifier
{
    Task NotifyMonthUpdated(DateTime date, CancellationToken cancellationToken = default);
    Task NotifyMonthsUpdated(IEnumerable<DateTime> dates, CancellationToken cancellationToken = default);
}

public sealed class BudgetMonthUpdateNotifier(IHubContext<BudgetMonthHub> hubContext) : IBudgetMonthUpdateNotifier
{
    public Task NotifyMonthUpdated(DateTime date, CancellationToken cancellationToken = default)
        => NotifyMonthsUpdated([date], cancellationToken);

    public async Task NotifyMonthsUpdated(IEnumerable<DateTime> dates, CancellationToken cancellationToken = default)
    {
        var months = dates
            .Select(x => BudgetMonthHub.ToMonthKey(x.StartOfMonth()))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var month in months)
        {
            await hubContext.Clients
                .Group(BudgetMonthHub.ToGroupName(month))
                .SendAsync(BudgetMonthHub.MonthUpdatedEvent, month, cancellationToken);
        }
    }
}

public sealed class NullBudgetMonthUpdateNotifier : IBudgetMonthUpdateNotifier
{
    public static readonly NullBudgetMonthUpdateNotifier Instance = new();

    private NullBudgetMonthUpdateNotifier()
    {
    }

    public Task NotifyMonthUpdated(DateTime date, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyMonthsUpdated(IEnumerable<DateTime> dates, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
