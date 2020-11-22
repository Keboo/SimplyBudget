using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;

namespace SimplyBudget.ViewModels
{
    public class BudgetHistoryDetailsViewModel
    {
        public string Amount { get; }

        public string ExpenseCategoryName { get; }

        public BudgetHistoryDetailsViewModel(TransactionItem item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            Amount = item.Amount.FormatCurrency();
            ExpenseCategoryName = item.ExpenseCategory?.Name;
        }

        public BudgetHistoryDetailsViewModel(ExpenseCategoryItemDetail item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            Amount = item.Amount.FormatCurrency();
            ExpenseCategoryName = item.ExpenseCategory?.Name;
        }

        public BudgetHistoryDetailsViewModel(IncomeItem item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            Amount = $"({item.Amount.FormatCurrency()})";
            ExpenseCategoryName = item.ExpenseCategory?.Name;
        }

        public BudgetHistoryDetailsViewModel(string amount, string expenseCategoryName)
        {
            Amount = amount;
            ExpenseCategoryName = expenseCategoryName;
        }
    }
}