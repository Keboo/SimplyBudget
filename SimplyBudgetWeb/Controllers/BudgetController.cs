using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/budget")]
public class BudgetController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<BudgetResponse> Get([FromQuery] DateTime? month)
    {
        var monthDate = (month ?? DateTime.Today).StartOfMonth();
        var start = monthDate.StartOfMonth();
        var end = monthDate.EndOfMonth();

        var categories = await context.ExpenseCategories
            .Where(c => !c.IsHidden)
            .ToListAsync();

        var categoryDtos = new List<BudgetCategoryDto>();

        foreach (var category in categories)
        {
            var items = await context.GetCategoryItemDetails(category);
            var monthItems = await context.GetCategoryItemDetails(category, start, end);

            int monthlyExpenses = monthItems
                .Where(x => !x.IgnoreBudget && x.Amount < 0 &&
                    !(x.ExpenseCategoryItem?.IsTransfer ?? false))
                .Sum(x => -x.Amount);

            int monthlyAllocations = monthItems
                .Where(x => (!x.IgnoreBudget || (x.ExpenseCategoryItem?.IsTransfer ?? false)) && x.Amount > 0)
                .Sum(x => x.Amount);

            categoryDtos.Add(new BudgetCategoryDto(
                Id: category.ID,
                Name: category.Name,
                CategoryName: category.CategoryName,
                AccountId: category.AccountID,
                BudgetedAmount: category.BudgetedAmount,
                BudgetedPercentage: category.BudgetedPercentage,
                CurrentBalance: category.CurrentBalance,
                Cap: category.Cap,
                IsHidden: category.IsHidden,
                UsePercentage: category.UsePercentage,
                MonthlyExpenses: monthlyExpenses,
                MonthlyAllocations: monthlyAllocations,
                ThreeMonthAverage: CalculateAverage(items, monthDate, 3),
                SixMonthAverage: CalculateAverage(items, monthDate, 6),
                TwelveMonthAverage: CalculateAverage(items, monthDate, 12)
            ));
        }

        int totalFixed = categoryDtos.Where(c => !c.UsePercentage).Sum(c => c.BudgetedAmount);
        int totalPercentage = categoryDtos.Sum(c => c.BudgetedPercentage);
        int totalBudget = totalPercentage > 0 && totalPercentage < 100
            ? totalFixed * 100 / (100 - totalPercentage)
            : totalFixed;

        return new BudgetResponse(
            TotalBudget: totalBudget,
            Month: monthDate.ToString("yyyy-MM"),
            Categories: categoryDtos
        );
    }

    private static int CalculateAverage(IList<ExpenseCategoryItemDetail> categoryItems, DateTime month, int numMonths)
    {
        var rangeStart = month.AddMonths(-numMonths);
        var items = categoryItems
            .Where(x => !x.IgnoreBudget && x.Amount < 0 &&
                x.ExpenseCategoryItem?.Date >= rangeStart &&
                x.ExpenseCategoryItem?.Date < month)
            .GroupBy(x => x.ExpenseCategoryItem?.Date.StartOfMonth())
            .Select(g => g.Sum(x => -x.Amount))
            .ToList();
        return items.Any() ? (int)items.Average() : 0;
    }
}

public record BudgetResponse(int TotalBudget, string Month, List<BudgetCategoryDto> Categories);

public record BudgetCategoryDto(
    int Id,
    string? Name,
    string? CategoryName,
    int? AccountId,
    int BudgetedAmount,
    int BudgetedPercentage,
    int CurrentBalance,
    int? Cap,
    bool IsHidden,
    bool UsePercentage,
    int MonthlyExpenses,
    int MonthlyAllocations,
    int ThreeMonthAverage,
    int SixMonthAverage,
    int TwelveMonthAverage
);
