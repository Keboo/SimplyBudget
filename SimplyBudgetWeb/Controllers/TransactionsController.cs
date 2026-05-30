using Microsoft.AspNetCore.Mvc;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(BudgetWebContext context) : ControllerBase
{
    [HttpPost("transaction")]
    public async Task<IActionResult> AddTransaction([FromBody] TransactionRequest request)
    {
        await context.AddTransaction(
            request.Description,
            request.Date,
            request.Items.Select(i => (i.Amount, i.ExpenseCategoryId)).ToArray());
        return StatusCode(201);
    }

    [HttpPost("income")]
    public async Task<IActionResult> AddIncome([FromBody] TransactionRequest request)
    {
        await context.AddIncome(
            request.Description,
            request.Date,
            request.Items.Select(i => (i.Amount, i.ExpenseCategoryId)).ToArray());
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
        return StatusCode(201);
    }
}

public record TransactionItemRequest(int ExpenseCategoryId, int Amount);

public record TransactionRequest(string Description, DateTime Date, TransactionItemRequest[] Items);

public record TransferRequest(
    string Description,
    DateTime Date,
    int Amount,
    int FromCategoryId,
    int ToCategoryId
);
