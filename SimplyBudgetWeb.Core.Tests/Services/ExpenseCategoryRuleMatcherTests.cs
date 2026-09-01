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

    [Test]
    public async Task Match_WhenNotTransaction_ReturnsNoNotes()
    {
        List<ExpenseCategoryRule> rules =
        [
            new ExpenseCategoryRule
            {
                Name = "Payroll rule",
                RuleRegex = "PAYROLL",
                Notes = "Employer deposit"
            }
        ];

        var result = ExpenseCategoryRuleMatcher.Match(rules, "PAYROLL ACH", isTransaction: false);

        await Assert.That(result.SuggestedCategoryId).IsNull();
        await Assert.That(result.Notes).IsNull();
    }

    [Test]
    public async Task Match_WhenRuleHasNotesWithoutCategory_ReturnsNotes()
    {
        List<ExpenseCategoryRule> rules =
        [
            new ExpenseCategoryRule
            {
                Name = "Unknown business",
                RuleRegex = "SQ \\*",
                Notes = "Square payment - check the merchant",
                ExpenseCategoryID = null
            }
        ];

        var result = ExpenseCategoryRuleMatcher.Match(rules, "SQ *COFFEE SHOP", isTransaction: true);

        await Assert.That(result.SuggestedCategoryId).IsNull();
        await Assert.That(result.Notes).IsEqualTo("Square payment - check the merchant");
    }

    [Test]
    public async Task Match_WhenMultipleRulesMatch_CombinesDistinctNotes()
    {
        List<ExpenseCategoryRule> rules =
        [
            new ExpenseCategoryRule { RuleRegex = "Costco", Notes = "Warehouse club" },
            new ExpenseCategoryRule { RuleRegex = "gas", Notes = "Fuel", ExpenseCategoryID = 42 },
            new ExpenseCategoryRule { RuleRegex = "Costco gas", Notes = "warehouse club" }
        ];

        var result = ExpenseCategoryRuleMatcher.Match(rules, "Costco gas", isTransaction: true);

        await Assert.That(result.SuggestedCategoryId).IsEqualTo(42);
        await Assert.That(result.Notes)
            .IsEqualTo($"Warehouse club{Environment.NewLine}Fuel");
    }

    [Test]
    public async Task Match_WhenNoRuleHasNotes_ReturnsNullNotes()
    {
        List<ExpenseCategoryRule> rules =
        [
            new ExpenseCategoryRule { RuleRegex = "Costco", ExpenseCategoryID = 42, Notes = "   " }
        ];

        var result = ExpenseCategoryRuleMatcher.Match(rules, "Costco gas", isTransaction: true);

        await Assert.That(result.SuggestedCategoryId).IsEqualTo(42);
        await Assert.That(result.Notes).IsNull();
    }
}
