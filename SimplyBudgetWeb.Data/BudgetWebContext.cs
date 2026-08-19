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

    /// <summary>
    /// Web-only tables: pending expenses (and their assignees) are not part of the shared
    /// <see cref="BudgetContext"/> because they do not apply to the desktop client.
    /// </summary>
    public DbSet<PendingExpense> PendingExpenses => Set<PendingExpense>();
    public DbSet<PendingExpenseAssignee> PendingExpenseAssignees => Set<PendingExpenseAssignee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PendingExpense>()
            .HasIndex(x => x.Date);

        modelBuilder.Entity<PendingExpense>()
            .HasOne(x => x.Assignee)
            .WithMany(x => x.PendingExpenses)
            .HasForeignKey(x => x.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PendingExpense>()
            .HasOne(x => x.SuggestedCategory)
            .WithMany()
            .HasForeignKey(x => x.SuggestedCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PendingExpenseAssignee>()
            .HasIndex(x => x.ObjectId)
            .IsUnique();
    }
}
