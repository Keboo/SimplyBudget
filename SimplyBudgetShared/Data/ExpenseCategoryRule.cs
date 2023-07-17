using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data;

[Table("ExpenseCategoryRules")]
public class ExpenseCategoryRule : BaseItem
{
    public string? Name { get; set; }
    public string? RuleRegex { get; set; }

    public int? ExpenseCategoryID { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
}
