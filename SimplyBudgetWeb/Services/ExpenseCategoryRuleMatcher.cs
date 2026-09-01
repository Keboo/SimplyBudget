using SimplyBudgetShared.Data;
using System.Text.RegularExpressions;

namespace SimplyBudgetWeb.Services;

public static class ExpenseCategoryRuleMatcher
{
    public static int? GetSuggestedCategoryId(IEnumerable<ExpenseCategoryRule> rules, string? description, bool isTransaction)
    {
        if (!isTransaction)
            return null;

        var descriptionToMatch = description ?? "";
        int? suggestedCategoryId = null;

        foreach (var rule in rules)
        {
            if (rule.ExpenseCategoryID is null || string.IsNullOrWhiteSpace(rule.RuleRegex))
                continue;

            if (Regex.IsMatch(descriptionToMatch, rule.RuleRegex, RegexOptions.IgnoreCase))
                suggestedCategoryId = rule.ExpenseCategoryID;
        }

        return suggestedCategoryId;
    }
}
