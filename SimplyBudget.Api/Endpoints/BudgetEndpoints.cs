using Microsoft.EntityFrameworkCore;
using SimplyBudget.Data;

namespace SimplyBudget.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budget").WithTags("Budget");

        group.MapGet("/", async (int? year, int? month, IDbContextFactory<AppDbContext> factory) =>
        {
            var targetDate = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
            var nextMonth = targetDate.AddMonths(1);

            await using var db = await factory.CreateDbContextAsync();

            var categories = await db.ExpenseCategories
                .Where(c => !c.IsHidden)
                .OrderBy(c => c.CategoryName)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var categoryIds = categories.Select(c => c.ID).ToList();

            var monthlySpending = await db.ExpenseCategoryItemDetails
                .Where(d => !d.IgnoreBudget
                    && d.ExpenseCategoryItem!.Date >= targetDate
                    && d.ExpenseCategoryItem.Date < nextMonth
                    && categoryIds.Contains(d.ExpenseCategoryId))
                .GroupBy(d => d.ExpenseCategoryId)
                .Select(g => new { CategoryId = g.Key, Total = g.Sum(d => d.Amount) })
                .ToListAsync();

            var spendingMap = monthlySpending.ToDictionary(x => x.CategoryId, x => x.Total);

            // Calculate total budget from the formula: totalBudget = fixedTotal * 100 / (100 - percentageTotal)
            int fixedTotal = categories.Where(c => c.BudgetedPercentage == 0).Sum(c => c.BudgetedAmount);
            int percentageTotal = categories.Sum(c => c.BudgetedPercentage);
            int totalBudget = percentageTotal < 100 ? fixedTotal * 100 / (100 - percentageTotal) : fixedTotal;

            var items = categories.Select(c =>
            {
                int budgetedAmount = c.BudgetedPercentage > 0
                    ? (int)(totalBudget * c.BudgetedPercentage / 100.0)
                    : c.BudgetedAmount;
                spendingMap.TryGetValue(c.ID, out int spent);
                int remaining = budgetedAmount - spent;
                return new BudgetCategoryDto(
                    c.ID, c.Name, c.CategoryName,
                    c.BudgetedAmount, c.BudgetedPercentage, budgetedAmount,
                    spent, remaining, c.CurrentBalance, c.Cap);
            }).ToList();

            return Results.Ok(new BudgetOverviewDto(targetDate.Year, targetDate.Month, totalBudget, items));
        });

        return app;
    }
}

public record BudgetCategoryDto(
    int Id, string? Name, string? CategoryName,
    int BudgetedAmount, int BudgetedPercentage, int EffectiveBudgetAmount,
    int SpentThisMonth, int RemainingThisMonth, int CurrentBalance, int? Cap);

public record BudgetOverviewDto(int Year, int Month, int TotalBudget, IList<BudgetCategoryDto> Categories);
