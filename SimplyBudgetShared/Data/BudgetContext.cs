using Microsoft.EntityFrameworkCore;

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

        public BudgetContext(DbContextOptions<BudgetContext> options)
            : base(options)
        {
            
        }
    }
}
