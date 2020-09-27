using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SimplyBudgetShared.Data
{
    public class BudgetContextFactory : IDesignTimeDbContextFactory<BudgetContext>
    {
        public BudgetContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BudgetContext>();
            optionsBuilder.UseSqlite("Data Source=data.db");

            return new BudgetContext(Microsoft.Toolkit.Mvvm.Messaging.Messenger.Default, optionsBuilder.Options);
        }
    }
}
