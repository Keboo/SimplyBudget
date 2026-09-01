using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.DataTransfer;

public static class BudgetDataPortabilityService
{
    public static async Task<BudgetDataExportPackage> ExportAsync(
        BudgetContext context,
        string source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new BudgetDataExportPackage
        {
            ExportedAtUtc = DateTime.UtcNow,
            Source = source,
            Accounts = await context.Accounts
                .AsNoTracking()
                .OrderBy(x => x.ID)
                .Select(x => new BudgetDataExportAccount(x.ID, x.Name, x.ValidatedDate, x.IsDefault))
                .ToListAsync(cancellationToken),
            Categories = await context.ExpenseCategories
                .AsNoTracking()
                .OrderBy(x => x.ID)
                .Select(x => new BudgetDataExportCategory(
                    x.ID,
                    x.Name,
                    x.CategoryName,
                    x.AccountID,
                    x.BudgetedAmount,
                    x.BudgetedPercentage,
                    x.CurrentBalance,
                    x.Cap,
                    x.IsHidden))
                .ToListAsync(cancellationToken),
            Items = await context.ExpenseCategoryItems
                .AsNoTracking()
                .OrderBy(x => x.ID)
                .Select(x => new BudgetDataExportItem(x.ID, x.Date, x.Description, x.Notes))
                .ToListAsync(cancellationToken),
            ItemDetails = await context.ExpenseCategoryItemDetails
                .AsNoTracking()
                .OrderBy(x => x.ID)
                .Select(x => new BudgetDataExportItemDetail(
                    x.ID,
                    x.ExpenseCategoryItemId,
                    x.ExpenseCategoryId,
                    x.Amount,
                    x.IgnoreBudget))
                .ToListAsync(cancellationToken),
            Rules = await context.ExpenseCategoryRules
                .AsNoTracking()
                .OrderBy(x => x.ID)
                .Select(x => new BudgetDataExportRule(x.ID, x.Name, x.RuleRegex, x.ExpenseCategoryID))
                .ToListAsync(cancellationToken),
            Metadata = await context.Metadatas
                .AsNoTracking()
                .OrderBy(x => x.ID)
                .Select(x => new BudgetDataExportMetadata(x.ID, x.Key, x.Value))
                .ToListAsync(cancellationToken)
        };
    }

    public static async Task ImportAsync(
        BudgetContext context,
        BudgetDataExportPackage exportPackage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exportPackage);

        if (exportPackage.FormatVersion > BudgetDataExportPackage.CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported export format version '{exportPackage.FormatVersion}'.");
        }

        // SQL Server's retrying execution strategy doesn't allow manually-created
        // transactions (they aren't safe to retry as-is). The whole operation must
        // instead be wrapped in a delegate passed to the execution strategy so that,
        // on a transient failure, the entire transaction (including the deletes and
        // inserts below) is retried as a single unit.
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.ExpenseCategoryItemDetails.ExecuteDeleteAsync(cancellationToken);
            await context.ExpenseCategoryItems.ExecuteDeleteAsync(cancellationToken);
            await context.ExpenseCategoryRules.ExecuteDeleteAsync(cancellationToken);
            await context.ExpenseCategories.ExecuteDeleteAsync(cancellationToken);
            await context.Accounts.ExecuteDeleteAsync(cancellationToken);
            await context.Metadatas.ExecuteDeleteAsync(cancellationToken);
            context.ChangeTracker.Clear();

            var accountSeed = (exportPackage.Accounts ?? []).OrderBy(x => x.Id).ToList();
            var importedAccounts = accountSeed.Select(x => new Account
            {
                Name = x.Name,
                ValidatedDate = x.ValidatedDate,
            }).ToList();
            context.Accounts.AddRange(importedAccounts);
            await context.SaveChangesAsync(cancellationToken);

            var accountIdMap = accountSeed
                .Zip(importedAccounts)
                .ToDictionary(x => x.First.Id, x => x.Second.ID);

            if (importedAccounts.Count > 0)
            {
                var preferredDefault = accountSeed.FirstOrDefault(x => x.IsDefault)?.Id ?? accountSeed[0].Id;
                if (!accountIdMap.TryGetValue(preferredDefault, out var mappedDefaultId))
                {
                    mappedDefaultId = importedAccounts[0].ID;
                }

                await context.Accounts
                    .ExecuteUpdateAsync(
                        updates => updates.SetProperty(account => account.IsDefault, false),
                        cancellationToken);

                var updatedRows = await context.Accounts
                    .Where(account => account.ID == mappedDefaultId)
                    .ExecuteUpdateAsync(
                        updates => updates.SetProperty(account => account.IsDefault, true),
                        cancellationToken);

                if (updatedRows != 1)
                {
                    throw new InvalidOperationException("Failed to locate imported default account.");
                }
            }

            var categorySeed = (exportPackage.Categories ?? []).OrderBy(x => x.Id).ToList();
            var importedCategories = categorySeed.Select(x => new ExpenseCategory
            {
                Name = x.Name,
                CategoryName = x.CategoryName,
                AccountID = ResolveOptionalReference(accountIdMap, x.AccountId, "account"),
                BudgetedAmount = x.BudgetedAmount,
                BudgetedPercentage = x.BudgetedPercentage,
                CurrentBalance = 0,
                Cap = x.Cap,
                IsHidden = x.IsHidden
            }).ToList();
            context.ExpenseCategories.AddRange(importedCategories);
            await context.SaveChangesAsync(cancellationToken);

            var categoryIdMap = categorySeed
                .Zip(importedCategories)
                .ToDictionary(x => x.First.Id, x => x.Second.ID);

            var itemSeed = (exportPackage.Items ?? []).OrderBy(x => x.Id).ToList();
            var importedItems = itemSeed.Select(x => new ExpenseCategoryItem
            {
                Date = x.Date,
                Description = x.Description,
                Notes = x.Notes
            }).ToList();
            context.ExpenseCategoryItems.AddRange(importedItems);
            await context.SaveChangesAsync(cancellationToken);

            var itemIdMap = itemSeed
                .Zip(importedItems)
                .ToDictionary(x => x.First.Id, x => x.Second.ID);

            var importedDetails = (exportPackage.ItemDetails ?? [])
                .OrderBy(x => x.Id)
                .Select(x => new ExpenseCategoryItemDetail
                {
                    ExpenseCategoryItemId = ResolveRequiredReference(itemIdMap, x.ExpenseCategoryItemId, "item"),
                    ExpenseCategoryId = ResolveRequiredReference(categoryIdMap, x.ExpenseCategoryId, "category"),
                    Amount = x.Amount,
                    IgnoreBudget = x.IgnoreBudget
                })
                .ToList();
            context.ExpenseCategoryItemDetails.AddRange(importedDetails);
            await context.SaveChangesAsync(cancellationToken);

            // Preserve exported category balances exactly. Some historical data sets
            // cannot be reconstructed solely from item details, so rebuilding from
            // details during import can drift from the source snapshot.
            for (var i = 0; i < importedCategories.Count; i++)
            {
                importedCategories[i].CurrentBalance = categorySeed[i].CurrentBalance;
            }
            await context.SaveChangesAsync(cancellationToken);

            var importedRules = (exportPackage.Rules ?? [])
                .OrderBy(x => x.Id)
                .Select(x => new ExpenseCategoryRule
                {
                    Name = x.Name,
                    RuleRegex = x.RuleRegex,
                    ExpenseCategoryID = ResolveOptionalReference(categoryIdMap, x.ExpenseCategoryId, "category")
                })
                .ToList();
            context.ExpenseCategoryRules.AddRange(importedRules);

            var importedMetadata = (exportPackage.Metadata ?? [])
                .OrderBy(x => x.Id)
                .Select(x => new Metadata
                {
                    Key = x.Key,
                    Value = x.Value
                })
                .ToList();
            context.Metadatas.AddRange(importedMetadata);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private static int ResolveRequiredReference(
        IReadOnlyDictionary<int, int> idMap,
        int sourceId,
        string entityName)
    {
        if (idMap.TryGetValue(sourceId, out var mappedId))
        {
            return mappedId;
        }

        throw new InvalidOperationException(
            $"The export references {entityName} id '{sourceId}' but it was not found.");
    }

    private static int? ResolveOptionalReference(
        IReadOnlyDictionary<int, int> idMap,
        int? sourceId,
        string entityName)
    {
        if (sourceId is null)
        {
            return null;
        }

        if (idMap.TryGetValue(sourceId.Value, out var mappedId))
        {
            return mappedId;
        }

        throw new InvalidOperationException(
            $"The export references {entityName} id '{sourceId}' but it was not found.");
    }
}
