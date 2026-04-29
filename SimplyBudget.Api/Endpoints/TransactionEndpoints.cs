using Microsoft.EntityFrameworkCore;
using SimplyBudget.Data;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions").WithTags("Transactions");

        group.MapGet("/", async (
            int? year, int? month, string? search,
            [Microsoft.AspNetCore.Mvc.FromQuery] int[]? categoryIds,
            int? accountId,
            IDbContextFactory<AppDbContext> factory) =>
        {
            var targetDate = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
            var nextMonth = targetDate.AddMonths(1);

            await using var db = await factory.CreateDbContextAsync();

            var query = db.ExpenseCategoryItems
                .Include(i => i.Details)!
                    .ThenInclude(d => d.ExpenseCategory)
                .Where(i => i.Date >= targetDate && i.Date < nextMonth);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.Description!.Contains(search));

            if (categoryIds?.Length > 0)
                query = query.Where(i => i.Details!.Any(d => categoryIds.Contains(d.ExpenseCategoryId)));

            if (accountId.HasValue)
                query = query.Where(i => i.Details!.Any(d => d.ExpenseCategory!.AccountID == accountId));

            var items = await query.OrderByDescending(i => i.Date).ToListAsync();

            var dtos = items.Select(i => new TransactionDto(
                i.ID, i.Date, i.Description,
                i.Details?.Select(d => new TransactionDetailDto(
                    d.ID, d.ExpenseCategoryId, d.ExpenseCategory?.Name, d.Amount, d.IgnoreBudget))
                    .ToList() ?? [])).ToList();

            return Results.Ok(dtos);
        });

        group.MapPost("/", async (CreateTransactionRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            if (req.Items.Count == 0)
                return Results.BadRequest("At least one line item is required.");

            await using var db = await factory.CreateDbContextAsync();

            var item = new ExpenseCategoryItem
            {
                Date = req.Date,
                Description = req.Description,
                Details = req.Items.Select(i => new ExpenseCategoryItemDetail
                {
                    ExpenseCategoryId = i.CategoryId,
                    Amount = -Math.Abs(i.Amount),
                    IgnoreBudget = req.IgnoreBudget
                }).ToList()
            };

            db.ExpenseCategoryItems.Add(item);
            await db.SaveChangesAsync();

            return Results.Created($"/api/transactions/{item.ID}", item.ID);
        });

        group.MapPost("/income", async (CreateIncomeRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            if (req.Items.Count == 0)
                return Results.BadRequest("At least one allocation is required.");

            await using var db = await factory.CreateDbContextAsync();

            var item = new ExpenseCategoryItem
            {
                Date = req.Date,
                Description = req.Description,
                Details = req.Items.Select(i => new ExpenseCategoryItemDetail
                {
                    ExpenseCategoryId = i.CategoryId,
                    Amount = Math.Abs(i.Amount),
                    IgnoreBudget = req.IgnoreBudget
                }).ToList()
            };

            db.ExpenseCategoryItems.Add(item);
            await db.SaveChangesAsync();

            return Results.Created($"/api/transactions/{item.ID}", item.ID);
        });

        group.MapPost("/transfer", async (CreateTransferRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            if (req.FromCategoryId == req.ToCategoryId)
                return Results.BadRequest("From and To categories must be different.");
            if (req.Amount <= 0)
                return Results.BadRequest("Amount must be positive.");

            await using var db = await factory.CreateDbContextAsync();

            var item = new ExpenseCategoryItem
            {
                Date = req.Date,
                Description = req.Description,
                Details =
                [
                    new ExpenseCategoryItemDetail
                    {
                        ExpenseCategoryId = req.FromCategoryId,
                        Amount = -req.Amount,
                        IgnoreBudget = req.IgnoreBudget
                    },
                    new ExpenseCategoryItemDetail
                    {
                        ExpenseCategoryId = req.ToCategoryId,
                        Amount = req.Amount,
                        IgnoreBudget = req.IgnoreBudget
                    }
                ]
            };

            db.ExpenseCategoryItems.Add(item);
            await db.SaveChangesAsync();

            return Results.Created($"/api/transactions/{item.ID}", item.ID);
        });

        group.MapDelete("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var item = await db.ExpenseCategoryItems.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.ExpenseCategoryItems.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

public record TransactionDto(int Id, DateTime Date, string? Description, IList<TransactionDetailDto> Details);
public record TransactionDetailDto(int Id, int CategoryId, string? CategoryName, int Amount, bool IgnoreBudget);
public record LineItemRequest(int CategoryId, int Amount);
public record CreateTransactionRequest(DateTime Date, string Description, bool IgnoreBudget, IList<LineItemRequest> Items);
public record CreateIncomeRequest(DateTime Date, string Description, bool IgnoreBudget, IList<LineItemRequest> Items);
public record CreateTransferRequest(DateTime Date, string? Description, bool IgnoreBudget, int FromCategoryId, int ToCategoryId, int Amount);
