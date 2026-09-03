using Microsoft.AspNetCore.Mvc;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(
    BudgetWebContext context,
    IBudgetMonthUpdateNotifier? budgetMonthUpdateNotifier = null,
    IBudgetMonthDataCache? budgetMonthDataCache = null) : ControllerBase
{
    private readonly IBudgetMonthUpdateNotifier budgetMonthUpdates = budgetMonthUpdateNotifier ?? NullBudgetMonthUpdateNotifier.Instance;
    private readonly IBudgetMonthDataCache monthDataCache = budgetMonthDataCache ?? NullBudgetMonthDataCache.Instance;

    [HttpPost("transaction")]
    public async Task<IActionResult> AddTransaction([FromBody] TransactionRequest request)
    {
        var item = await context.AddTransaction(
            request.Description,
            request.Date,
            request.Items.Select(i => (i.Amount, i.ExpenseCategoryId)).ToArray());

        var notes = NormalizeNotes(request.Notes);
        if (notes is not null)
        {
            item.Notes = notes;
            await context.SaveChangesAsync();
        }
        monthDataCache.InvalidateMonth(request.Date);
        await budgetMonthUpdates.NotifyMonthUpdated(request.Date);
        return StatusCode(201);
    }

    [HttpPost("income")]
    public async Task<IActionResult> AddIncome([FromBody] TransactionRequest request)
    {
        var item = await context.AddIncome(
            request.Description,
            request.Date,
            request.Items.Select(i => (i.Amount, i.ExpenseCategoryId)).ToArray());

        var notes = NormalizeNotes(request.Notes);
        if (notes is not null)
        {
            item.Notes = notes;
            await context.SaveChangesAsync();
        }
        monthDataCache.InvalidateMonth(request.Date);
        await budgetMonthUpdates.NotifyMonthUpdated(request.Date);
        return StatusCode(201);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> AddTransfer([FromBody] TransferRequest request)
    {
        var fromCategory = await context.ExpenseCategories.FindAsync(request.FromCategoryId);
        if (fromCategory is null) return NotFound($"Category {request.FromCategoryId} not found.");

        var toCategory = await context.ExpenseCategories.FindAsync(request.ToCategoryId);
        if (toCategory is null) return NotFound($"Category {request.ToCategoryId} not found.");

        await context.AddTransfer(request.Description, request.Date, request.Amount, fromCategory, toCategory);
        monthDataCache.InvalidateMonth(request.Date);
        await budgetMonthUpdates.NotifyMonthUpdated(request.Date);
        return StatusCode(201);
    }

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}

public record TransactionItemRequest(int ExpenseCategoryId, int Amount);

public record TransactionRequest(string Description, DateTime Date, TransactionItemRequest[] Items, string? Notes = null);

public record TransferRequest(
    string Description,
    DateTime Date,
    int Amount,
    int FromCategoryId,
    int ToCategoryId
);
