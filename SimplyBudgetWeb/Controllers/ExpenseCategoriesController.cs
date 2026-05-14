using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;

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
            query = query.Where(c => c.Name != null && c.Name.Contains(search));

        var categories = await query.ToListAsync();
        return categories.Select(ToDto).ToArray();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseCategoryDto>> GetById(int id)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();
        return ToDto(category);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseCategoryDto>> Create([FromBody] ExpenseCategoryRequest request)
    {
        var category = new ExpenseCategory
        {
            Name = request.Name,
            CategoryName = request.CategoryName,
            BudgetedAmount = request.BudgetedAmount,
            BudgetedPercentage = request.BudgetedPercentage,
            Cap = request.Cap,
            AccountID = request.AccountId,
        };
        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.ID }, ToDto(category));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseCategoryDto>> Update(int id, [FromBody] ExpenseCategoryRequest request)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = request.Name;
        category.CategoryName = request.CategoryName;
        category.BudgetedAmount = request.BudgetedAmount;
        category.BudgetedPercentage = request.BudgetedPercentage;
        category.Cap = request.Cap;
        category.AccountID = request.AccountId;

        await context.SaveChangesAsync();
        return ToDto(category);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsHidden = true;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<ExpenseCategoryDto>> Restore(int id)
    {
        var category = await context.ExpenseCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsHidden = false;
        await context.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    private static ExpenseCategoryDto ToDto(ExpenseCategory c) => new(
        Id: c.ID,
        Name: c.Name,
        CategoryName: c.CategoryName,
        AccountId: c.AccountID,
        BudgetedAmount: c.BudgetedAmount,
        BudgetedPercentage: c.BudgetedPercentage,
        CurrentBalance: c.CurrentBalance,
        Cap: c.Cap,
        IsHidden: c.IsHidden,
        UsePercentage: c.UsePercentage
    );
}

public record ExpenseCategoryDto(
    int Id,
    string? Name,
    string? CategoryName,
    int? AccountId,
    int BudgetedAmount,
    int BudgetedPercentage,
    int CurrentBalance,
    int? Cap,
    bool IsHidden,
    bool UsePercentage
);

public record ExpenseCategoryRequest(
    string? Name,
    string? CategoryName,
    int BudgetedAmount,
    int BudgetedPercentage,
    int? Cap,
    int? AccountId
);
