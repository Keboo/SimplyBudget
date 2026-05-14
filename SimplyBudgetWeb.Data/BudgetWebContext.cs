using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;

namespace SimplyBudgetWeb.Data;

/// <summary>
/// EF Core DbContext for the web application.
/// Inherits all entity definitions and balance-adjustment hooks from BudgetContext,
/// configured for Azure SQL Server with the SimplyBudget schema.
/// </summary>
public class BudgetWebContext(DbContextOptions<BudgetWebContext> options)
    : BudgetContext(WeakReferenceMessenger.Default, options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("SimplyBudget");
        base.OnModelCreating(modelBuilder);
    }
}
