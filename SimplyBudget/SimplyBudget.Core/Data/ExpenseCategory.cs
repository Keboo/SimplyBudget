using SimplyBudget.Core.Utilities;

namespace SimplyBudget.Core.Data;

public class ExpenseCategory : BaseItem, IBeforeCreate
{
    public string? CategoryName { get; set; }

    public int? AccountID { get; set; }
    public Account? Account { get; set; }

    public string? Name { get; set; }
    public int BudgetedPercentage { get; set; }
    public int BudgetedAmount { get; set; }
    public int CurrentBalance { get; set; }
    public int? Cap { get; set; }
    public bool IsHidden { get; set; }

    public bool UsePercentage => BudgetedPercentage > 0;

    public string GetBudgetedDisplayString()
    {
        return UsePercentage
                   ? BudgetedPercentage.FormatPercentage()
                   : BudgetedAmount.FormatCurrency();
    }

    public async Task BeforeCreate(BudgetContext context)
    {
        if (AccountID is null && Account is null)
        {
            Account = await context.GetDefaultAccountAsync();
        }
    }
}