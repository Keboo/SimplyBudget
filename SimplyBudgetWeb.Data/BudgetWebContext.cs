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
    /// <summary>
    /// The database schema used for all tables in this context, including
    /// the EF Core migrations history table (see <see cref="BudgetWebContextDesignTimeFactory"/>
    /// and <c>DependencyInjection.AddDatabase</c>).
    /// </summary>
    public const string Schema = "SimplyBudget";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
