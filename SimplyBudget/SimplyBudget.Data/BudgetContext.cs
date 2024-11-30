using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

namespace SimplyBudget.Data;

public class BudgetContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<ExpenseCategoryItem> ExpenseCategoryItems => Set<ExpenseCategoryItem>();
    public DbSet<ExpenseCategoryItemDetail> ExpenseCategoryItemDetails => Set<ExpenseCategoryItemDetail>();
    public DbSet<Metadata> Metadatas => Set<Metadata>();
    public DbSet<ExpenseCategoryRule> ExpenseCategoryRules => Set<ExpenseCategoryRule>();

    public BudgetContext()
        : this("Data Source=data.db")
    { }

    public BudgetContext(string connectionString)
        : this (new DbContextOptionsBuilder<BudgetContext>().UseSqlite(connectionString).Options)
    { }

    public BudgetContext(DbContextOptions<BudgetContext> options)
        : base(options)
    {
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        return base.DisposeAsync();
    }

    public static string GetFilePathFromConnectionString(string connectionString)
    {
        if (Regex.Match(connectionString, @"Data Source=([^;]+)") is Match match &&
            match.Success)
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(match.Groups[1].Value));
        }
        throw new InvalidOperationException($"Failed to resolve file path from connection string '{connectionString}'");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .LogTo(x => Debug.WriteLine(x))
            .EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>()
            .HasIndex(x => x.IsDefault);

        modelBuilder.Entity<ExpenseCategory>()
            .HasIndex(x => x.CategoryName);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var notifications = new List<Action>();
        var foo = ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();
        foreach (var entity in PumpItems(() => ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged), EntityComparer.Instance))
        {
            if (entity.Entity is IBeforeCreate beforeCreate &&
                entity.State == EntityState.Added)
            {
                await beforeCreate.BeforeCreate(this);
            }
            else if (entity.Entity is IBeforeRemove beforeRemove &&
                     entity.State == EntityState.Deleted)
            {
                await beforeRemove.BeforeRemove(this);
            }

            EventType type = entity.State switch
            {
                EntityState.Added => EventType.Created,
                EntityState.Deleted => EventType.Deleted,
                EntityState.Modified => EventType.Updated,
                _ => EventType.None
            };
            if (type == EventType.None) continue;

            switch (entity.Entity)
            {
                case ExpenseCategory category:
                    notifications.Add(() => Messenger.Send(new DatabaseEvent<ExpenseCategory>(category, type)));
                    break;
                case ExpenseCategoryItem item:
                    notifications.Add(() => Messenger.Send(new DatabaseEvent<ExpenseCategoryItem>(item, type)));
                    break;
                case ExpenseCategoryItemDetail detailItem:
                    notifications.Add(() => Messenger.Send(new DatabaseEvent<ExpenseCategoryItemDetail>(detailItem, type)));
                    break;
                case Account account:
                    notifications.Add(() => Messenger.Send(new DatabaseEvent<Account>(account, type)));
                    break;
            }
        }

        var rv = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        foreach (var notification in notifications)
        {
            notification.Invoke();
        }

        return rv;
    }

    public override int SaveChanges() => throw new InvalidOperationException($"Must use {nameof(SaveChangesAsync)}");

    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw new InvalidOperationException($"Must use {nameof(SaveChangesAsync)}");

    private static IEnumerable<T> PumpItems<T>(Func<IEnumerable<T>> items, IEqualityComparer<T>? comparer = null)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var seen = new HashSet<T>(comparer);

        bool loop;
        do
        {
            loop = false;
            foreach (var item in items())
            {
                if (seen.Add(item))
                {
                    yield return item;
                    loop = true;
                    break;
                }
            }
        }
        while (loop);
    }

    private class EntityComparer : IEqualityComparer<EntityEntry>
    {
        public static EntityComparer Instance { get; } = new EntityComparer();

        public bool Equals([AllowNull] EntityEntry x, [AllowNull] EntityEntry y)
            => Equals(x?.Entity, y?.Entity);

        public int GetHashCode([DisallowNull] EntityEntry obj)
            => (obj.Entity?.GetHashCode() ?? 0, obj.Entity?.GetType()).GetHashCode();
    }
}
