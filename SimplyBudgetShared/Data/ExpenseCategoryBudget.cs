using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data;

[Table("ExpenseCategoryBudget")]
public class ExpenseCategoryBudget
{
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;

    // MinValue denotes the known baseline, not a fabricated historical change.
    public DateTime EffectiveMonth { get; set; }
    public int BudgetedAmount { get; set; }
    public int BudgetedPercentage { get; set; }
}