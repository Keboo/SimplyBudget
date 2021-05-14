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

            var categoryItems = await context.GetCategoryItemDetails(expenseCategory, month.StartOfMonth(), month.EndOfMonth());

            var rv = new ExpenseCategoryViewModelEx(expenseCategory.ID);
            SetProperties(expenseCategory, rv);
            rv.MonthlyExpenses = categoryItems
                .Where(x => x.Amount < 0 && x.ExpenseCategoryItem?.IsTransfer == false)
                .Sum(x => -x.Amount);

            rv.MonthlyAllocations = categoryItems
                .Where(x => x.Amount > 0 || x.ExpenseCategoryItem?.IsTransfer == true)
                .Sum(x => x.Amount);
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

        private string? _budgetedAmountDisplay;
        public string? BudgetedAmountDisplay
        {
            get => _budgetedAmountDisplay;
            set => SetProperty(ref _budgetedAmountDisplay, value);
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value) && value)
                {
                    EditingName = Name;
                    EditingCategory = CategoryName;
                }
            }
        }

        private string? _editingName;
        public string? EditingName
        {
            get => _editingName;
            set => SetProperty(ref _editingName, value);
        }

        private string? _editingCatergory;
        public string? EditingCategory
        {
            get => _editingCatergory;
            set => SetProperty(ref _editingCatergory, value);
        }
    }
}