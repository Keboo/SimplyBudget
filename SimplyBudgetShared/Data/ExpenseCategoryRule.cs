using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data;

[Table("ExpenseCategoryRules")]
public class ExpenseCategoryRule : BaseItem
{
    public string? Name { get; set; }
    public string? RuleRegex { get; set; }

    /// <summary>
    /// Optional note applied to matching transactions. Allows a rule to add context
    /// (for example what a business is) without needing to set an expense category.
    /// </summary>
    public string? Notes { get; set; }

    public int? ExpenseCategoryID { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
}
