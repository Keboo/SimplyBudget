using Microsoft.EntityFrameworkCore;
using SimplyBudget.Data;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Api.Endpoints;

public static class CategoryRuleEndpoints
{
    public static IEndpointRouteBuilder MapCategoryRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/category-rules").WithTags("CategoryRules");

        group.MapGet("/", async (IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var rules = await db.ExpenseCategoryRules
                .OrderBy(r => r.Name)
                .Select(r => new CategoryRuleDto(r.ID, r.Name, r.RuleRegex, r.ExpenseCategoryID))
                .ToListAsync();
            return Results.Ok(rules);
        });

        group.MapPost("/", async (CreateCategoryRuleRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var rule = new ExpenseCategoryRule
            {
                Name = req.Name,
                RuleRegex = req.RuleRegex,
                ExpenseCategoryID = req.ExpenseCategoryId
            };
            db.ExpenseCategoryRules.Add(rule);
            await db.SaveChangesAsync();
            return Results.Created($"/api/category-rules/{rule.ID}",
                new CategoryRuleDto(rule.ID, rule.Name, rule.RuleRegex, rule.ExpenseCategoryID));
        });

        group.MapPut("/{id:int}", async (int id, UpdateCategoryRuleRequest req, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var rule = await db.ExpenseCategoryRules.FindAsync(id);
            if (rule is null) return Results.NotFound();
            rule.Name = req.Name;
            rule.RuleRegex = req.RuleRegex;
            rule.ExpenseCategoryID = req.ExpenseCategoryId;
            await db.SaveChangesAsync();
            return Results.Ok(new CategoryRuleDto(rule.ID, rule.Name, rule.RuleRegex, rule.ExpenseCategoryID));
        });

        group.MapDelete("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var rule = await db.ExpenseCategoryRules.FindAsync(id);
            if (rule is null) return Results.NotFound();
            db.ExpenseCategoryRules.Remove(rule);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Match description against rules to suggest a category
        group.MapGet("/match", async (string description, IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var rules = await db.ExpenseCategoryRules
                .Where(r => r.RuleRegex != null)
                .ToListAsync();

            foreach (var rule in rules)
            {
                try
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(description,
                        rule.RuleRegex!, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        return Results.Ok(new { CategoryId = rule.ExpenseCategoryID });
                    }
                }
                catch { /* ignore invalid regex */ }
            }

            return Results.Ok(new { CategoryId = (int?)null });
        });

        return app;
    }
}

public record CategoryRuleDto(int Id, string? Name, string? RuleRegex, int? ExpenseCategoryId);
public record CreateCategoryRuleRequest(string Name, string? RuleRegex, int? ExpenseCategoryId);
public record UpdateCategoryRuleRequest(string Name, string? RuleRegex, int? ExpenseCategoryId);
