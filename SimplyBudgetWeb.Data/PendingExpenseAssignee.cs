using System.ComponentModel.DataAnnotations.Schema;
using SimplyBudgetShared.Data;

namespace SimplyBudgetWeb.Data;

/// <summary>
/// A person a <see cref="PendingExpense"/> can optionally be assigned to.
/// This is a web-only concept (not shared with the desktop client).
/// </summary>
[Table("PendingExpenseAssignee")]
public class PendingExpenseAssignee : BaseItem
{
    public string Name { get; set; } = string.Empty;

    public List<PendingExpense>? PendingExpenses { get; set; }
}
