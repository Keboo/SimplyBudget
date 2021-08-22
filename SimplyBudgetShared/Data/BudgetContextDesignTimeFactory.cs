using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace SimplyBudgetShared.Data
{
    public class BudgetContextDesignTimeFactory : IDesignTimeDbContextFactory<BudgetContext>
    {
        public BudgetContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BudgetContext>();
            optionsBuilder.UseSqlite("Data Source=data.db");

            return new BudgetContext(WeakReferenceMessenger.Default, optionsBuilder.Options);
        }
    }
}
