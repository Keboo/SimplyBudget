using System.ComponentModel.DataAnnotations.Schema;
using SimplyBudgetShared.Data;

namespace SimplyBudgetWeb.Data;

/// <summary>
/// A person a <see cref="PendingExpense"/> can optionally be assigned to.
/// This is a web-only concept (not shared with the desktop client). Rows are created and kept
/// up to date automatically whenever a user signs in (see <c>CurrentUserSyncMiddleware</c>) —
/// there is no manual "add assignee" flow, so this doubles as the list of known application users.
/// </summary>
[Table("PendingExpenseAssignee")]
public class PendingExpenseAssignee : BaseItem
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether <see cref="Name"/> was explicitly customized by the user in Settings.
    /// When true, login-time profile sync keeps the custom name instead of replacing it with the
    /// identity-provider claim value.
    /// </summary>
    public bool IsNameCustomized { get; set; }

    /// <summary>
    /// The unique object ID (Entra ID "oid" claim) of the signed-in user this row was created
    /// for. Used to match a returning user regardless of display name changes.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime LastLoginUtc { get; set; } = DateTime.UtcNow;

    public List<PendingExpense>? PendingExpenses { get; set; }
}
