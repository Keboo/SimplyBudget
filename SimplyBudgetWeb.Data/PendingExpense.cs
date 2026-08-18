using System.ComponentModel.DataAnnotations.Schema;
using SimplyBudgetShared.Data;

namespace SimplyBudgetWeb.Data;

/// <summary>
/// A receipt/transaction that has occurred but has not yet been categorized (or split) into
/// a real <see cref="ExpenseCategoryItem"/>. This is a web-only concept (not shared with the
/// desktop client): pending expenses are typically created in bulk from a CSV import and are
/// worked off one at a time from the "Pending Expenses" page until they are converted into
/// real expense items.
/// </summary>
[Table("PendingExpense")]
public class PendingExpense : BaseItem
{
    private DateTime _date;
    public DateTime Date
    {
        get => _date;
        set => _date = value.Date; // Ensure that we only capture the date
    }

    public string? Description { get; set; }

    /// <summary>
    /// Always a positive number of cents. Whether this represents money leaving or entering
    /// an account is tracked separately via <see cref="IsDebit"/>.
    /// </summary>
    public int Amount { get; set; }

    public bool IsDebit { get; set; } = true;

    /// <summary>
    /// Free-form notes an assignee (or anyone else) can leave for additional context while the
    /// expense is still pending categorization.
    /// </summary>
    public string? Notes { get; set; }

    public int? AssigneeId { get; set; }
    public PendingExpenseAssignee? Assignee { get; set; }

    /// <summary>
    /// Best-guess expense category (e.g. from an import rule match), used to pre-populate the
    /// category when this pending expense is later converted into a real expense item.
    /// </summary>
    public int? SuggestedCategoryId { get; set; }
    public ExpenseCategory? SuggestedCategory { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
