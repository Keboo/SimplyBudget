using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.Data;
using SimplyBudgetShared.Data;
using System.Globalization;

namespace SimplyBudget.Api.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/import").WithTags("Import");

        group.MapPost("/", async (
            IFormFile file,
            IDbContextFactory<AppDbContext> factory) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            });

            var rows = new List<ImportRowDto>();
            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var row = new ImportRowDto(
                    csv.TryGetField<DateTime>("Date", out var date) ? date : DateTime.Today,
                    csv.TryGetField<string>("Description", out var desc) ? desc ?? "" : "",
                    csv.TryGetField<decimal>("Amount", out var amount) ? amount : 0m,
                    null // CategoryId to be assigned by client
                );
                rows.Add(row);
            }

            return Results.Ok(rows);
        }).DisableAntiforgery();

        group.MapPost("/confirm", async (
            ConfirmImportRequest req,
            IDbContextFactory<AppDbContext> factory) =>
        {
            if (req.Transactions.Count == 0)
                return Results.BadRequest("No transactions to import.");

            await using var db = await factory.CreateDbContextAsync();

            // Match descriptions against category rules
            var rules = await db.ExpenseCategoryRules
                .Where(r => r.RuleRegex != null && r.ExpenseCategoryID != null)
                .ToListAsync();

            var items = new List<ExpenseCategoryItem>();
            foreach (var t in req.Transactions)
            {
                int? categoryId = t.CategoryId;

                if (categoryId is null)
                {
                    foreach (var rule in rules)
                    {
                        try
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(
                                t.Description, rule.RuleRegex!,
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            {
                                categoryId = rule.ExpenseCategoryID;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (categoryId is null) continue; // skip unmatched

                int amountCents = (int)Math.Round(Math.Abs(t.Amount) * 100);
                if (t.Amount > 0) amountCents = -amountCents; // positive bank amount = expense (debit)

                items.Add(new ExpenseCategoryItem
                {
                    Date = t.Date,
                    Description = t.Description,
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            ExpenseCategoryId = categoryId.Value,
                            Amount = amountCents,
                            IgnoreBudget = false
                        }
                    ]
                });
            }

            db.ExpenseCategoryItems.AddRange(items);
            await db.SaveChangesAsync();

            return Results.Ok(new { Imported = items.Count, Skipped = req.Transactions.Count - items.Count });
        });

        return app;
    }
}

public record ImportRowDto(DateTime Date, string Description, decimal Amount, int? CategoryId);
public record ConfirmImportRequest(IList<ImportRowDto> Transactions);
