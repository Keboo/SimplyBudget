using Microsoft.EntityFrameworkCore;
using SimplyBudget.Data;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Api.Endpoints;

public static class ExpenseCategoryEndpoints
{
    public static IEndpointRouteBuilder MapExpenseCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expense-categories").WithTags("ExpenseCategories");

        group.MapGet("/", async (bool? showHidden, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var query = db.ExpenseCategories.AsQueryable();
            if (showHidden != true)
                query = query.Where(c => !c.IsHidden);
            var categories = await query
                .OrderBy(c => c.CategoryName)
                .ThenBy(c => c.Name)
                .Select(c => new ExpenseCategoryDto(c.ID, c.Name, c.CategoryName,
                    c.BudgetedAmount, c.BudgetedPercentage, c.CurrentBalance,
                    c.Cap, c.IsHidden, c.AccountID))
                .ToListAsync();
            return Results.Ok(categories);
        });

        group.MapGet("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var c = await db.ExpenseCategories.FindAsync(id);
            if (c is null) return Results.NotFound();
            return Results.Ok(new ExpenseCategoryDto(c.ID, c.Name, c.CategoryName,
                c.BudgetedAmount, c.BudgetedPercentage, c.CurrentBalance, c.Cap, c.IsHidden, c.AccountID));
        });

        group.MapPost("/", async (CreateExpenseCategoryRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var category = new ExpenseCategory
            {
                Name = req.Name,
                CategoryName = req.CategoryName,
                BudgetedAmount = req.BudgetedAmount,
                BudgetedPercentage = req.BudgetedPercentage,
                Cap = req.Cap,
                AccountID = req.AccountId
            };
            db.ExpenseCategories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/expense-categories/{category.ID}",
                new ExpenseCategoryDto(category.ID, category.Name, category.CategoryName,
                    category.BudgetedAmount, category.BudgetedPercentage, category.CurrentBalance,
                    category.Cap, category.IsHidden, category.AccountID));
        });

        group.MapPut("/{id:int}", async (int id, UpdateExpenseCategoryRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var category = await db.ExpenseCategories.FindAsync(id);
            if (category is null) return Results.NotFound();
            category.Name = req.Name;
            category.CategoryName = req.CategoryName;
            category.BudgetedAmount = req.BudgetedAmount;
            category.BudgetedPercentage = req.BudgetedPercentage;
            category.Cap = req.Cap;
            category.AccountID = req.AccountId;
            await db.SaveChangesAsync();
            return Results.Ok(new ExpenseCategoryDto(category.ID, category.Name, category.CategoryName,
                category.BudgetedAmount, category.BudgetedPercentage, category.CurrentBalance,
                category.Cap, category.IsHidden, category.AccountID));
        });

        group.MapPatch("/{id:int}/hide", async (int id, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var category = await db.ExpenseCategories.FindAsync(id);
            if (category is null) return Results.NotFound();
            category.IsHidden = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPatch("/{id:int}/show", async (int id, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var category = await db.ExpenseCategories.FindAsync(id);
            if (category is null) return Results.NotFound();
            category.IsHidden = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var category = await db.ExpenseCategories.FindAsync(id);
            if (category is null) return Results.NotFound();
            db.ExpenseCategories.Remove(category);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

public record ExpenseCategoryDto(
    int Id, string? Name, string? CategoryName,
    int BudgetedAmount, int BudgetedPercentage,
    int CurrentBalance, int? Cap, bool IsHidden, int? AccountId);

public record CreateExpenseCategoryRequest(
    string Name, string? CategoryName,
    int BudgetedAmount, int BudgetedPercentage,
    int? Cap, int? AccountId);

public record UpdateExpenseCategoryRequest(
    string Name, string? CategoryName,
    int BudgetedAmount, int BudgetedPercentage,
    int? Cap, int? AccountId);
