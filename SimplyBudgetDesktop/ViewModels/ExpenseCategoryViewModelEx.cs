using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
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

            var rv = new ExpenseCategoryViewModelEx(expenseCategory.ID)
            {
                Accounts = await context.Accounts.ToListAsync(),
                Account = expenseCategory.Account ?? await context.Accounts.FirstOrDefaultAsync(x => x.ID == expenseCategory.AccountID)
            };
            SetProperties(expenseCategory, rv);
            rv.MonthlyExpenses = categoryItems
                .Where(x => x.IgnoreBudget == false)
                .Where(x => x.Amount < 0 && x.ExpenseCategoryItem?.IsTransfer == false)
                .Sum(x => -x.Amount);

            rv.MonthlyAllocations = categoryItems
                .Where(x => x.IgnoreBudget == false)
                .Where(x => x.Amount > 0 || x.ExpenseCategoryItem?.IsTransfer == true)
                .Sum(x => x.Amount);
            rv.BudgetedAmountDisplay = expenseCategory.GetBudgetedDisplayString();
            return rv;
        }

        private ExpenseCategoryViewModelEx(int expenseCategoryID)
            : base(expenseCategoryID)
        { }

        public IList<Account>? Accounts { get; init; }

        private Account? _account;
        public Account? Account
        {
            get => _account;
            set => SetProperty(ref _account, value);
        }

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
                    EditIsAmountType = BudgetedPercentage <= 0;
                    EditAmount = BudgetedPercentage > 0 ? BudgetedPercentage : BudgetedAmount;
                    EditingCap = Cap;
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

        private bool _editIsAmountType;
        public bool EditIsAmountType
        {
            get => _editIsAmountType;
            set => SetProperty(ref _editIsAmountType, value);
        }

        private int _editAmount;
        public int EditAmount
        {
            get => _editAmount;
            set => SetProperty(ref _editAmount, value);
        }

        private int? _editingCap;
        public int? EditingCap
        {
            get => _editingCap;
            set => SetProperty(ref _editingCap, value);
        }
    }
}