using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SimplyBudget.Data;

public class BudgetContextDesignTimeFactory : IDesignTimeDbContextFactory<BudgetContext>
{
    public BudgetContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetContext>();
        //optionsBuilder.UseSqlite("Data Source=data.db");

        return new BudgetContext(optionsBuilder.Options);
    }
}
