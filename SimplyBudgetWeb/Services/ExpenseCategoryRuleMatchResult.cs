namespace SimplyBudgetWeb.Services;

/// <summary>
/// The combined result of applying the expense category rules to a single transaction.
/// </summary>
/// <param name="SuggestedCategoryId">
/// The category suggested by the last matching rule that specified a category, or null when no
/// matching rule specified one.
/// </param>
/// <param name="Notes">
/// The notes contributed by all matching rules, joined by new lines, or null when no matching
/// rule specified notes.
/// </param>
public record ExpenseCategoryRuleMatchResult(int? SuggestedCategoryId, string? Notes)
{
    public static ExpenseCategoryRuleMatchResult None { get; } = new(null, null);
}
