using SimplyBudgetShared.Data;
using System.Text.RegularExpressions;

namespace SimplyBudgetWeb.Services;

public static class ExpenseCategoryRuleMatcher
{
    public static int? GetSuggestedCategoryId(IEnumerable<ExpenseCategoryRule> rules, string? description, bool isTransaction, int? amount = null)
        => Match(rules, description, isTransaction, amount).SuggestedCategoryId;

    /// <summary>
    /// Applies the rules to a transaction description. A rule matches on its regex (and optional
    /// amount range) alone, so a rule may contribute notes without setting an expense category
    /// (and vice versa).
    /// </summary>
    /// <param name="amount">
    /// The transaction amount in cents, used to evaluate rules that specify an amount range.
    /// Rules with an amount range never match when the amount is unknown.
    /// </param>
    public static ExpenseCategoryRuleMatchResult Match(
        IEnumerable<ExpenseCategoryRule> rules,
        string? description,
        bool isTransaction,
        int? amount = null)
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

            if (!rule.IsAmountInRange(amount))
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
