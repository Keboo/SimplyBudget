using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data;

/// <summary>
/// A data driven rule that displays an external link next to any transaction (or pending expense)
/// whose description matches <see cref="RuleRegex"/>. This replaces the previously hard coded
/// Amazon link and works much like <see cref="ExpenseCategoryRule"/>, except that a match results
/// in a link to <see cref="Url"/> rather than a category suggestion.
/// </summary>
[Table("ExternalLinkRule")]
public class ExternalLinkRule : BaseItem
{
    /// <summary>
    /// Display name for the rule, also used as the accessible label/tooltip for the link.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Case insensitive regular expression matched against the item description.
    /// </summary>
    public string? RuleRegex { get; set; }

    /// <summary>
    /// The absolute URL opened when the link is clicked.
    /// </summary>
    public string? Url { get; set; }
}
