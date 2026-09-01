using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Utilities;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/expense-categories")]
public class ExpenseCategoriesController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ExpenseCategoryDto[]> GetAll(
        [FromQuery] bool includeHidden = false,
        [FromQuery] string? search = null)
    {
        var query = context.ExpenseCategories.AsQueryable();

        if (!includeHidden)
            query = query.Where(c => !c.IsHidden);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchText = search.Trim();
            var hasSearchAmount = SearchAmountParser.TryParseAmountInCents(searchText, out var searchAmountInCents);
            var searchAmountAbs = Math.Abs(searchAmountInCents);

            query = query.Where(c =>
                (c.Name != null && c.Name.Contains(searchText)) ||
                (c.Description != null && c.Description.Contains(searchText)) ||
                (c.CategoryName != null && c.CategoryName.Contains(searchText)) ||
                (hasSearchAmount &&
                 (Math.Abs(c.BudgetedAmount) == searchAmountAbs ||
                  Math.Abs(c.CurrentBalance) == searchAmountAbs ||
                  (c.Cap.HasValue && Math.Abs(c.Cap.Value) == searchAmountAbs))));
        }

        var categories = await query.ToListAsync();
        var idsWithItems = await GetIdsWithItemsAsync(categories.Select(c => c.ID));
        return categories.Select(c => ToDto(c, idsWithItems.Contains(c.ID))).ToArray();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseCategoryDto>> GetById(int id)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();
        return ToDto(category, await context.HasItemsAsync(category));
    }

    /// <summary>
    /// Returns, for every visible (non-hidden) category, the month's currently allocated amount
    /// and remaining budget amount used by the income-allocation UI.
    /// </summary>
    [HttpGet("remaining-budget")]
    public async Task<Dictionary<int, RemainingBudgetDto>> GetRemainingBudget([FromQuery] DateTime? month)
    {
        var monthDate = (month ?? DateTime.Today).StartOfMonth();

        var categories = await context.ExpenseCategories
            .Where(c => !c.IsHidden)
            .ToListAsync();

        var result = new Dictionary<int, RemainingBudgetDto>();
        foreach (var category in categories)
        {
            var currentAmount = category.UsePercentage
                ? 0
                : await context.GetCurrentMonthAllocatedAmount(category, monthDate);

            result[category.ID] = new RemainingBudgetDto(
                CurrentAmount: currentAmount,
                RemainingAmount: BudgetContextExtensions.CalculateRemainingBudgetAmount(category, currentAmount));
        }
        return result;
    }

    [HttpGet("{id}/monthly-expenses")]
    public async Task<ActionResult<ExpenseCategoryMonthlyExpensesDto>> GetMonthlyExpenses(
        int id,
        [FromQuery] DateTime? month,
        [FromQuery] int months = 12)
    {
        if (months is < 1 or > 24)
            return BadRequest("months must be between 1 and 24.");

        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        var monthDate = (month ?? DateTime.Today).StartOfMonth();
        var rangeStart = monthDate.AddMonths(-(months - 1)).StartOfMonth();
        var rangeEnd = monthDate.EndOfMonth();

        var transferItemIds = await context.ExpenseCategoryItems
            .Where(x => x.Date >= rangeStart && x.Date <= rangeEnd)
            .Where(x => x.Details!.Count == 2 && x.Details.Sum(d => d.Amount) == 0)
            .Select(x => x.ID)
            .ToListAsync();

        var monthlyExpenses = await context.ExpenseCategoryItemDetails
            .Where(x => x.ExpenseCategoryId == id)
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

        var history = Enumerable.Range(0, months)
            .Select(offset =>
            {
                var date = rangeStart.AddMonths(offset);
                return new ExpenseCategoryMonthlyExpensePointDto(
                    Month: date.ToString("yyyy-MM"),
                    Amount: monthlyTotals.GetValueOrDefault(date, 0));
            })
            .ToArray();

        return new ExpenseCategoryMonthlyExpensesDto(
            ExpenseCategoryId: category.ID,
            Name: category.Name,
            BudgetedAmount: category.BudgetedAmount,
            BudgetedPercentage: category.BudgetedPercentage,
            UsePercentage: category.UsePercentage,
            Months: history);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseCategoryDto>> Create([FromBody] ExpenseCategoryRequest request)
    {
        var category = new ExpenseCategory
        {
            Name = request.Name,
            Description = request.Description,
            CategoryName = request.CategoryName,
            BudgetedAmount = request.BudgetedAmount,
            BudgetedPercentage = request.BudgetedPercentage,
            Cap = request.Cap,
            AccountID = request.AccountId,
        };
        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.ID }, ToDto(category, hasItems: false));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseCategoryDto>> Update(int id, [FromBody] ExpenseCategoryRequest request)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = request.Name;
        category.Description = request.Description;
        category.CategoryName = request.CategoryName;
        category.BudgetedAmount = request.BudgetedAmount;
        category.BudgetedPercentage = request.BudgetedPercentage;
        category.Cap = request.Cap;
        category.AccountID = request.AccountId;

        await context.SaveChangesAsync();
        return ToDto(category, await context.HasItemsAsync(category));
    }

    /// <summary>
    /// Hides a category from normal views (e.g. the budget list) without deleting it or losing
    /// its transaction history. Use <see cref="Restore"/> to unhide it again.
    /// </summary>
    [HttpPost("{id}/hide")]
    public async Task<ActionResult<ExpenseCategoryDto>> Hide(int id)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsHidden = true;
        await context.SaveChangesAsync();
        return ToDto(category, await context.HasItemsAsync(category));
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<ExpenseCategoryDto>> Restore(int id)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsHidden = false;
        await context.SaveChangesAsync();
        return ToDto(category, await context.HasItemsAsync(category));
    }

    /// <summary>
    /// Permanently deletes a category. Only allowed when the category has no items posted
    /// against it and isn't referenced by an import rule; otherwise use <see cref="Hide"/>.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        if (!await context.CanDeleteAsync(category))
            return Conflict("This category has items (or is used by an import rule) and cannot be deleted. Hide it instead.");

        context.ExpenseCategories.Remove(category);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<HashSet<int>> GetIdsWithItemsAsync(IEnumerable<int> categoryIds)
    {
        var ids = categoryIds.ToList();
        if (ids.Count == 0) return [];

        var idsWithItems = await context.ExpenseCategoryItemDetails
            .Where(d => ids.Contains(d.ExpenseCategoryId))
            .Select(d => d.ExpenseCategoryId)
            .Distinct()
            .ToListAsync();
        return [.. idsWithItems];
    }

    private static ExpenseCategoryDto ToDto(ExpenseCategory c, bool hasItems) => new(
        Id: c.ID,
        Name: c.Name,
        Description: c.Description,
        CategoryName: c.CategoryName,
        AccountId: c.AccountID,
        BudgetedAmount: c.BudgetedAmount,
        BudgetedPercentage: c.BudgetedPercentage,
        CurrentBalance: c.CurrentBalance,
        Cap: c.Cap,
        IsHidden: c.IsHidden,
        UsePercentage: c.UsePercentage,
        HasItems: hasItems
    );
}

public record ExpenseCategoryDto(
    int Id,
    string? Name,
    string? Description,
    string? CategoryName,
    int? AccountId,
    int BudgetedAmount,
    int BudgetedPercentage,
    int CurrentBalance,
    int? Cap,
    bool IsHidden,
    bool UsePercentage,
    bool HasItems
);

public record ExpenseCategoryRequest(
    string? Name,
    string? Description,
    string? CategoryName,
    int BudgetedAmount,
    int BudgetedPercentage,
    int? Cap,
    int? AccountId
);

public record ExpenseCategoryMonthlyExpensesDto(
    int ExpenseCategoryId,
    string? Name,
    int BudgetedAmount,
    int BudgetedPercentage,
    bool UsePercentage,
    ExpenseCategoryMonthlyExpensePointDto[] Months
);

public record ExpenseCategoryMonthlyExpensePointDto(
    string Month,
    int Amount
);

public record RemainingBudgetDto(
    int CurrentAmount,
    int RemainingAmount
);
