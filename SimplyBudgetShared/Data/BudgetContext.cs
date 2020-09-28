using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Utilities.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public class BudgetContext : DbContext
    {
        static BudgetContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BudgetContext>();
            optionsBuilder.UseSqlite("Data Source=data.db");

            Instance = new BudgetContext(Microsoft.Toolkit.Mvvm.Messaging.Messenger.Default, optionsBuilder.Options);
        }

        public static BudgetContext Instance { get; }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<Income> Incomes => Set<Income>();
        public DbSet<IncomeItem> IncomeItems => Set<IncomeItem>();
        public DbSet<MetaData> MetaDatas => Set<MetaData>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
        public DbSet<Transfer> Transfers => Set<Transfer>();

        private IMessenger Messenger { get; }

        public BudgetContext(IMessenger messenger, DbContextOptions options)
            : base(options)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
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

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var notifications = new List<Action>();
            foreach (var entity in ChangeTracker.Entries())
            {
                if (entity.Entity is IBeforeCreate beforeCreate &&
                    entity.State == EntityState.Added)
                {
                    await beforeCreate.BeforeCreate(this);
                }

                EventType type = entity.State switch
                {
                    EntityState.Added => EventType.Created,
                    EntityState.Deleted => EventType.Deleted,
                    EntityState.Modified => EventType.Updated,
                    _ => EventType.None
                };
                if (type == EventType.None) continue;

                switch(entity.Entity)
                {
                    case ExpenseCategory category:
                        notifications.Add(() => Messenger.Send(new ExpenseCategoryEvent(this, category, type)));
                        break;
                    case Account account:
                        notifications.Add(() => Messenger.Send(new AccountEvent(this, account, type)));
                        break;
                    case Income income:
                        notifications.Add(() => Messenger.Send(new IncomeEvent(this, income, type)));
                        break;
                }
            }

            var rv = await base.SaveChangesAsync(cancellationToken);

            foreach(var notification in notifications)
            {
                notification.Invoke();
            }

            return rv;
        }
    }
}
