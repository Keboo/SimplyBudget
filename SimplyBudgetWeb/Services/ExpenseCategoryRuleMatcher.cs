using SimplyBudgetShared.Data;
using System.Text.RegularExpressions;

namespace SimplyBudgetWeb.Services;

public static class ExpenseCategoryRuleMatcher
{
    public static int? GetSuggestedCategoryId(IEnumerable<ExpenseCategoryRule> rules, string? description, bool isTransaction)
        => Match(rules, description, isTransaction).SuggestedCategoryId;

    /// <summary>
    /// Applies the rules to a transaction description. A rule matches on its regex alone, so a
    /// rule may contribute notes without setting an expense category (and vice versa).
    /// </summary>
    public static ExpenseCategoryRuleMatchResult Match(
        IEnumerable<ExpenseCategoryRule> rules,
        string? description,
        bool isTransaction)
    {
        if (!isTransaction)
            return ExpenseCategoryRuleMatchResult.None;

        var descriptionToMatch = description ?? "";
        int? suggestedCategoryId = null;
        List<string> notes = [];

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.RuleRegex))
                continue;

            if (!Regex.IsMatch(descriptionToMatch, rule.RuleRegex, RegexOptions.IgnoreCase))
                continue;

            if (rule.ExpenseCategoryID is not null)
                suggestedCategoryId = rule.ExpenseCategoryID;

            if (!string.IsNullOrWhiteSpace(rule.Notes))
            {
                var note = rule.Notes.Trim();
                if (!notes.Contains(note, StringComparer.OrdinalIgnoreCase))
                    notes.Add(note);
            }
        }

        return new ExpenseCategoryRuleMatchResult(
            suggestedCategoryId,
            notes.Count > 0 ? string.Join(Environment.NewLine, notes) : null);
    }
}
