using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Services;

internal static class CategorySpendingHistory
{
    public static async Task<ExpenseCategoryMonthlyExpensePointDto[]> GetMonthlyExpenses(
        BudgetWebContext context, int categoryId, DateTime month, int months)
    {
        var monthDate = month.StartOfMonth();
        var rangeStart = monthDate.AddMonths(-(months - 1));
        var rangeEnd = monthDate.EndOfMonth();

        var transferItemIds = await context.ExpenseCategoryItems
            .Where(x => x.Date >= rangeStart && x.Date <= rangeEnd)
            .Where(x => x.Details!.Count == 2 && x.Details.Sum(d => d.Amount) == 0)
            .Select(x => x.ID)
            .ToListAsync();

        var monthlyExpenses = await context.ExpenseCategoryItemDetails
            .Where(x => x.ExpenseCategoryId == categoryId)
            .Where(x => !x.IgnoreBudget && x.Amount < 0)
            .Where(x => x.ExpenseCategoryItem!.Date >= rangeStart && x.ExpenseCategoryItem.Date <= rangeEnd)
            .Where(x => !transferItemIds.Contains(x.ExpenseCategoryItemId))
            .GroupBy(x => new
            {
                x.ExpenseCategoryItem!.Date.Year,
                x.ExpenseCategoryItem.Date.Month,
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(x => -x.Amount),
            })
            .ToListAsync();

        var monthlyTotals = monthlyExpenses.ToDictionary(
            x => new DateTime(x.Year, x.Month, 1),
            x => x.Amount);

        return Enumerable.Range(0, months)
            .Select(offset =>
            {
                var date = rangeStart.AddMonths(offset);
                return new ExpenseCategoryMonthlyExpensePointDto(
                    Month: date.ToString("yyyy-MM"),
                    Amount: monthlyTotals.GetValueOrDefault(date, 0));
            })
            .ToArray();
    }
}
