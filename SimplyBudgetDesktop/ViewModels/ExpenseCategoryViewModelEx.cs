using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels
{
    public class ExpenseCategoryViewModelEx : ExpenseCategoryViewModel
    {
        public static async Task<ExpenseCategoryViewModelEx> Create(BudgetContext context, ExpenseCategory expenseCategory)
        {
            return await Create(context, expenseCategory, DateTime.Today);
        }

        public static async Task<ExpenseCategoryViewModelEx> Create(BudgetContext context, ExpenseCategory expenseCategory, DateTime month)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (expenseCategory is null) throw new ArgumentNullException(nameof(expenseCategory));
            var transactions = await context.GetTransactionItems(expenseCategory, month.StartOfMonth(), month.EndOfMonth());
            var incomeItems = await context.GetIncomeItems(expenseCategory, month.StartOfMonth(), month.EndOfMonth());

            var rv = new ExpenseCategoryViewModelEx(expenseCategory.ID);
            SetProperties(expenseCategory, rv);
            rv.MonthlyExpenses = transactions.Sum(x => x.Amount);
            rv.MonthlyAllocations = incomeItems.Sum(x => x.Amount);
            rv.BudgetedAmountDisplay = expenseCategory.GetBudgetedDisplayString();
            return rv;
        }

        private ExpenseCategoryViewModelEx(int expenseCategoryID)
            : base(expenseCategoryID)
        { }

        private int _monthlyExpenses;
        public int MonthlyExpenses
        {
            get => _monthlyExpenses;
            set => SetProperty(ref _monthlyExpenses, value);
        }

        private int _monthlyAllocations;
        public int MonthlyAllocations
        {
            get => _monthlyAllocations;
            set => SetProperty(ref _monthlyAllocations, value);
        }

        private string _budgetedAmountDisplay;
        public string BudgetedAmountDisplay
        {
            get => _budgetedAmountDisplay;
            set => SetProperty(ref _budgetedAmountDisplay, value);
        }
    }
}