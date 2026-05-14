using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SimplyBudgetWeb.Data;

public class BudgetWebContextDesignTimeFactory : IDesignTimeDbContextFactory<BudgetWebContext>
{
    public BudgetWebContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetWebContext>();
        // Connection string only used for migration generation, not applied to prod DB
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=SimplyBudget;Trusted_Connection=True;TrustServerCertificate=True;");
        return new BudgetWebContext(optionsBuilder.Options);
    }
}
