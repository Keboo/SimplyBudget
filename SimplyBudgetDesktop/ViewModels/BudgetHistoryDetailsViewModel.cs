using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels
{
    public class BudgetHistoryDetailsViewModel
    {
        public string Amount { get; }

        public string? ExpenseCategoryName { get; }

        public BudgetHistoryDetailsViewModel(ExpenseCategoryItemDetail item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            Amount = item.Amount.FormatCurrency();
            ExpenseCategoryName = item.ExpenseCategory?.Name;
        }
    }
}