using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<ExpenseCategoryItem> ExpenseCategoryItems => Set<ExpenseCategoryItem>();
    public DbSet<ExpenseCategoryItemDetail> ExpenseCategoryItemDetails => Set<ExpenseCategoryItemDetail>();
    public DbSet<ExpenseCategoryRule> ExpenseCategoryRules => Set<ExpenseCategoryRule>();
    public DbSet<Metadata> Metadatas => Set<Metadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("sb");

        modelBuilder.Entity<Account>()
            .HasIndex(x => x.IsDefault);

        modelBuilder.Entity<ExpenseCategory>()
            .HasIndex(x => x.CategoryName);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await RunBeforeCreateHooksAsync();
        await RunBeforeRemoveHooksAsync();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges() => throw new InvalidOperationException($"Must use {nameof(SaveChangesAsync)}");
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw new InvalidOperationException($"Must use {nameof(SaveChangesAsync)}");

    private async Task RunBeforeCreateHooksAsync()
    {
        foreach (var entry in ChangeTracker.Entries<ExpenseCategoryItemDetail>()
            .Where(e => e.State == EntityState.Added).ToList())
        {
            var detail = entry.Entity;
            int categoryId = detail.ExpenseCategory?.ID ?? detail.ExpenseCategoryId;
            var category = await Set<ExpenseCategory>().FindAsync(categoryId);
            if (category is null)
                throw new InvalidOperationException($"Could not find expense category {categoryId} for item detail");
            category.CurrentBalance += detail.Amount;
        }

        // Ensure new ExpenseCategory gets a default account if none specified
        foreach (var entry in ChangeTracker.Entries<ExpenseCategory>()
            .Where(e => e.State == EntityState.Added && e.Entity.AccountID is null && e.Entity.Account is null)
            .ToList())
        {
            var defaultAccount = await Accounts.Where(a => a.IsDefault).FirstOrDefaultAsync()
                ?? await Accounts.FirstOrDefaultAsync();
            if (defaultAccount is not null)
                entry.Entity.Account = defaultAccount;
        }
    }

    private async Task RunBeforeRemoveHooksAsync()
    {
        // When removing ExpenseCategoryItemDetail, reverse the balance
        foreach (var entry in ChangeTracker.Entries<ExpenseCategoryItemDetail>()
            .Where(e => e.State == EntityState.Deleted).ToList())
        {
            var detail = entry.Entity;
            if (await Set<ExpenseCategory>().FindAsync(detail.ExpenseCategoryId) is { } category)
            {
                category.CurrentBalance -= detail.Amount;
            }
        }

        // When removing an ExpenseCategoryItem, cascade delete its details
        foreach (var entry in ChangeTracker.Entries<ExpenseCategoryItem>()
            .Where(e => e.State == EntityState.Deleted).ToList())
        {
            var item = entry.Entity;
            await foreach (var detail in ExpenseCategoryItemDetails
                .Where(x => x.ExpenseCategoryItemId == item.ID).AsAsyncEnumerable())
            {
                Remove(detail);
            }
        }
    }
}
