using Microsoft.EntityFrameworkCore.Design;

namespace SimplyBudgetShared.Data;

public class BudgetContextDesignTimeFactory : IDesignTimeDbContextFactory<BudgetContext>
{
    public BudgetContext CreateDbContext(string[] args)
    {
        return new BudgetContext();
    }
}
