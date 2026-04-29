using Microsoft.EntityFrameworkCore;
using SimplyBudget.Data;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapGet("/", async (IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var accounts = await db.Accounts
                .OrderBy(a => a.Name)
                .Select(a => new AccountDto(
                    a.ID, a.Name, a.IsDefault,
                    a.ExpenseCategories!.Sum(c => c.CurrentBalance),
                    a.ValidatedDate))
                .ToListAsync();
            return Results.Ok(accounts);
        });

        group.MapPost("/", async (CreateAccountRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var account = new Account
            {
                Name = req.Name,
                ValidatedDate = req.ValidatedDate ?? DateTime.Today
            };
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
            return Results.Created($"/api/accounts/{account.ID}",
                new AccountDto(account.ID, account.Name, account.IsDefault, 0, account.ValidatedDate));
        });

        group.MapPut("/{id:int}", async (int id, UpdateAccountRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var account = await db.Accounts
                .Include(a => a.ExpenseCategories)
                .FirstOrDefaultAsync(a => a.ID == id);
            if (account is null) return Results.NotFound();
            account.Name = req.Name;
            if (req.ValidatedDate.HasValue)
                account.ValidatedDate = req.ValidatedDate.Value;
            await db.SaveChangesAsync();
            int balance = account.ExpenseCategories?.Sum(c => c.CurrentBalance) ?? 0;
            return Results.Ok(new AccountDto(account.ID, account.Name, account.IsDefault, balance, account.ValidatedDate));
        });

        group.MapDelete("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var account = await db.Accounts.FindAsync(id);
            if (account is null) return Results.NotFound();
            db.Accounts.Remove(account);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

public record AccountDto(int Id, string? Name, bool IsDefault, int CurrentBalance, DateTime ValidatedDate);
public record CreateAccountRequest(string Name, DateTime? ValidatedDate);
public record UpdateAccountRequest(string Name, DateTime? ValidatedDate);
