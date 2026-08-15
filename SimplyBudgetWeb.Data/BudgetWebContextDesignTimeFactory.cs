using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplyBudgetWeb.Data;

public class BudgetWebContextDesignTimeFactory : IDesignTimeDbContextFactory<BudgetWebContext>
{
    public BudgetWebContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetWebContext>();
        // Connection string only used for migration generation, not applied to prod DB
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=SimplyBudget;Trusted_Connection=True;TrustServerCertificate=True;",
            sqlOptions => sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, BudgetWebContext.Schema));
        return new BudgetWebContext(optionsBuilder.Options);
    }
}
