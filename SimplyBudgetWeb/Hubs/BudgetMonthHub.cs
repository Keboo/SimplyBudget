using System.Globalization;

using Microsoft.AspNetCore.SignalR;

namespace SimplyBudgetWeb.Hubs;

public class BudgetMonthHub : Hub
{
    public const string HubPath = "/hubs/budget-month-updates";
    public const string MonthUpdatedEvent = "MonthUpdated";
    public const string SubscribeMethod = "SubscribeToMonth";
    public const string UnsubscribeMethod = "UnsubscribeFromMonth";

    public Task SubscribeToMonth(string month)
    {
        var monthKey = NormalizeMonthKey(month);
        return Groups.AddToGroupAsync(Context.ConnectionId, ToGroupName(monthKey));
    }

    public Task UnsubscribeFromMonth(string month)
    {
        var monthKey = NormalizeMonthKey(month);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, ToGroupName(monthKey));
    }

    public static string ToMonthKey(DateTime date)
        => date.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public static string ToGroupName(string monthKey)
        => $"month:{NormalizeMonthKey(monthKey)}";

    private static string NormalizeMonthKey(string month)
    {
        if (string.IsNullOrWhiteSpace(month) ||
            !DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
        {
            throw new HubException("month must be in yyyy-MM format.");
        }

        return ToMonthKey(parsedMonth);
    }
}
