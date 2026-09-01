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

    /// <summary>
    /// Optional inclusive minimum transaction amount (in cents) required for this rule to match.
    /// </summary>
    public int? MinimumAmount { get; set; }

    /// <summary>
    /// Optional inclusive maximum transaction amount (in cents) required for this rule to match.
    /// </summary>
    public int? MaximumAmount { get; set; }

    public int? ExpenseCategoryID { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }

    /// <summary>
    /// Checks the (optional) amount range on this rule. Amounts are compared using their
    /// magnitude so callers do not need to normalize debit/credit signs. When the rule
    /// specifies a range but the amount is unknown, the rule does not match.
    /// </summary>
    public bool IsAmountInRange(int? amount)
    {
        if (MinimumAmount is null && MaximumAmount is null)
            return true;

        if (amount is null)
            return false;

        var magnitude = Math.Abs(amount.Value);

        if (MinimumAmount is { } min && magnitude < Math.Abs(min))
            return false;

        if (MaximumAmount is { } max && magnitude > Math.Abs(max))
            return false;

        return true;
    }
}
