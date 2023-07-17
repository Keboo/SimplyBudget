using CommunityToolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels;

public partial class ExpenseCategoryRuleViewModel : ObservableObject
{
    public int? ExistingRuleId { get; }

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _ruleRegex;

    [ObservableProperty]
    private int? _expenseCategoryId;

    public ExpenseCategoryRuleViewModel()
    {
        
    }

    public ExpenseCategoryRuleViewModel(ExpenseCategoryRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        ExistingRuleId = rule.ID;
        Name = rule.Name;
        RuleRegex = rule.RuleRegex;
        ExpenseCategoryId = rule.ExpenseCategoryID;
    }
}
