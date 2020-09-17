using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public class BudgetContext : DbContext
    {
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<Income> Incomes => Set<Income>();
        public DbSet<IncomeItem> IncomeItems => Set<IncomeItem>();
        public DbSet<MetaData> MetaDatas => Set<MetaData>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
        public DbSet<Transfer> Transfers => Set<Transfer>();

        public BudgetContext(DbContextOptions options)
            : base(options)
        { }

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
            //foreach(var account in ChangeTracker.Entries<Account>())
            //{
            //    var isDefaultProperty = account.Property<bool>(nameof(Account.IsDefault));
            //    if (isDefaultProperty.CurrentValue && 
            //        (isDefaultProperty.IsModified || account.State == EntityState.Added))
            //    {
            //        if (await Accounts.FirstOrDefaultAsync(x => x.IsDefault && x.ID != account.Entity.ID) is { } previousDefault)
            //        {
            //            previousDefault.IsDefault = false;
            //        }
            //    }
            //}
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
