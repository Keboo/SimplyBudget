using CommunityToolkit.Mvvm.Messaging;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using SimplyBudgetShared.Events;
using SimplyBudgetShared.Utilities;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SimplyBudgetShared.Data;

public class BudgetContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<ExpenseCategoryItem> ExpenseCategoryItems => Set<ExpenseCategoryItem>();
    public DbSet<ExpenseCategoryItemDetail> ExpenseCategoryItemDetails => Set<ExpenseCategoryItemDetail>();
    public DbSet<Metadata> Metadatas => Set<Metadata>();
    public DbSet<ExpenseCategoryRule> ExpenseCategoryRules => Set<ExpenseCategoryRule>();
    public DbSet<ExpenseCategoryBudget> ExpenseCategoryBudgets => Set<ExpenseCategoryBudget>();

    private IMessenger Messenger { get; }
    private readonly TimeProvider timeProvider;

    public BudgetContext()
        : this("Data Source=data.db")
    { }

    public BudgetContext(string connectionString)
        : this(WeakReferenceMessenger.Default,
              new DbContextOptionsBuilder<BudgetContext>().UseSqlite(connectionString).Options)
    { }

    public BudgetContext(IMessenger messenger, DbContextOptions options, TimeProvider? timeProvider = null)
        : base(options)
    {
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        this.timeProvider = timeProvider ?? TimeProvider.System;
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

        modelBuilder.Entity<ExpenseCategoryBudget>()
            .HasKey(x => new { x.ExpenseCategoryId, x.EffectiveMonth });
        modelBuilder.Entity<ExpenseCategoryBudget>()
            .HasOne(x => x.ExpenseCategory)
            .WithMany(x => x.BudgetHistory)
            .HasForeignKey(x => x.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await RecordBudgetChanges(cancellationToken);
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

    private async Task RecordBudgetChanges(CancellationToken cancellationToken)
    {
        var month = timeProvider.GetLocalNow().Date.StartOfMonth();
        foreach (var entry in ChangeTracker.Entries<ExpenseCategory>()
            .Where(x => x.State == EntityState.Modified).ToList())
        {
            var amount = entry.Property(x => x.BudgetedAmount);
            var percentage = entry.Property(x => x.BudgetedPercentage);
            if (amount.OriginalValue == amount.CurrentValue &&
                percentage.OriginalValue == percentage.CurrentValue)
                continue;

            await entry.Collection(x => x.BudgetHistory).LoadAsync(cancellationToken);
            var history = entry.Entity.BudgetHistory;
            if (history.Count == 0)
            {
                history.Add(new ExpenseCategoryBudget
                {
                    EffectiveMonth = DateTime.MinValue,
                    BudgetedAmount = amount.OriginalValue,
                    BudgetedPercentage = percentage.OriginalValue,
                });
            }

            var budget = history.SingleOrDefault(x => x.EffectiveMonth == month);
            if (budget is null)
            {
                budget = new ExpenseCategoryBudget { EffectiveMonth = month };
                history.Add(budget);
            }
            budget.BudgetedAmount = amount.CurrentValue;
            budget.BudgetedPercentage = percentage.CurrentValue;
        }
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
        var pending = new List<T>();

        // Each round collects every not-yet-seen item in a single pass over `items()`.
        // Processing an item (e.g. via IBeforeCreate/IBeforeRemove) may cause additional
        // entities to become tracked/changed, which subsequent rounds will pick up.
        // Doing this in batches (rather than restarting the full scan after every single
        // new item) keeps this roughly O(n * rounds) instead of O(n^2) for large batches.
        do
        {
            pending.Clear();
            foreach (var item in items())
            {
                if (seen.Add(item))
                {
                    pending.Add(item);
                }
            }

            foreach (var item in pending)
            {
                yield return item;
            }
        }
        while (pending.Count > 0);
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