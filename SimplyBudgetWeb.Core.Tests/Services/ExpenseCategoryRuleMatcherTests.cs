using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Services;

public class ExpenseCategoryRuleMatcherTests
{
    [Test]
    public async Task GetSuggestedCategoryId_WhenNotTransaction_ReturnsNull()
    {
        List<ExpenseCategoryRule> rules =
        [
            new ExpenseCategoryRule
            {
                Name = "Payroll rule",
                RuleRegex = "PAYROLL",
                ExpenseCategoryID = 7
            }
        ];

        var suggestedCategoryId = ExpenseCategoryRuleMatcher.GetSuggestedCategoryId(
            rules,
            "PAYROLL ACH",
            isTransaction: false);

        await Assert.That(suggestedCategoryId).IsNull();
    }

    [Test]
    public async Task GetSuggestedCategoryId_WhenTransaction_ReturnsLastMatchingCategorizedRule()
    {
        List<ExpenseCategoryRule> rules =
        [
            new ExpenseCategoryRule
            {
                Name = "Uncategorized",
                RuleRegex = "Costco",
                ExpenseCategoryID = null
            },
            new ExpenseCategoryRule
            {
                Name = "Categorized",
                RuleRegex = "Costco",
                ExpenseCategoryID = 42
            }
        ];

        var suggestedCategoryId = ExpenseCategoryRuleMatcher.GetSuggestedCategoryId(
            rules,
            "Costco gas",
            isTransaction: true);

        await Assert.That(suggestedCategoryId).IsEqualTo(42);
    }
}
