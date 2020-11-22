using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public class BudgetContext : DbContext
    {
        public const string FileName = "data.db";

        static BudgetContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BudgetContext>();
            optionsBuilder.UseSqlite($"Data Source={FileName}");

            Instance = new BudgetContext(WeakReferenceMessenger.Default, optionsBuilder.Options);
        }

        public static BudgetContext Instance { get; }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<ExpenseCategoryItem> ExpenseCategoryItems => Set<ExpenseCategoryItem>();
        public DbSet<ExpenseCategoryItemDetail> ExpenseCategoryItemDetails => Set<ExpenseCategoryItemDetail>();
        public DbSet<Metadata> Metadatas => Set<Metadata>();
        
        private IMessenger Messenger { get; }

        public BudgetContext(IMessenger messenger, DbContextOptions options)
            : base(options)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
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

            modelBuilder.Entity<TransactionItem>()
                .HasIndex(x => x.TransactionID);
            modelBuilder.Entity<TransactionItem>()
                .HasIndex(x => x.ExpenseCategoryID);

            modelBuilder.Entity<Transaction>()
                .HasIndex(x => x.Date);

            modelBuilder.Entity<Transfer>()
                .HasIndex(x => x.Date);
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
                        notifications.Add(() => Messenger.Send(new ExpenseCategoryEvent(this, category, type)));
                        break;
                    case ExpenseCategoryItem item:
                        notifications.Add(() => Messenger.Send(new DatabaseEvent<ExpenseCategoryItem>(this, item, type)));
                        break;
                    case ExpenseCategoryItemDetail detailItem:
                        notifications.Add(() => Messenger.Send(new DatabaseEvent<ExpenseCategoryItemDetail>(this, detailItem, type)));
                        break;
                    case Account account:
                        notifications.Add(() => Messenger.Send(new AccountEvent(this, account, type)));
                        break;
                    case Income income:
                        notifications.Add(() => Messenger.Send(new IncomeEvent(this, income, type)));
                        break;
                    case IncomeItem incomeItem:
                        notifications.Add(() => Messenger.Send(new IncomeItemEvent(this, incomeItem, type)));
                        break;
                    case Transaction transaction:
                        notifications.Add(() => Messenger.Send(new TransactionEvent(this, transaction, type)));
                        break;
                    case TransactionItem transactionItem:
                        notifications.Add(() => Messenger.Send(new TransactionItemEvent(this, transactionItem, type)));
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

        public static IEnumerable<T> PumpItems<T>(Func<IEnumerable<T>> items, IEqualityComparer<T>? comparer = null)
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
}
