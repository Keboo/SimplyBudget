using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("ExpenseCategoryItemDetail")]
    public class ExpenseCategoryItemDetail : BaseItem, IEquatable<ExpenseCategoryItemDetail>
    {
        public int ExpenseCategoryItemId { get; set; }
        public ExpenseCategoryItem? ExpenseCategoryItem { get; set; }

        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        public int Amount { get; set; }


    }

    [Table("ExpenseCategoryItem")]
    public class ExpenseCategoryItem : BaseItem, IBeforeRemove
    {
        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => _date = value.Date;  //Ensure that we only capture the date
        }

        public string? Description { get; set; }


        public List<ExpenseCategoryItemDetail>? Details { get; set; }

        public async Task BeforeRemove(BudgetContext context)
        {
            //await foreach (var item in context.IncomeItems.Where(x => x.IncomeID == ID).AsAsyncEnumerable())
            //{
            //    context.Remove(item);
            //}
        }
    }

    [Table("ExpenseCategory")]
    public class ExpenseCategory : BaseItem, IBeforeCreate
    {
        public string? CategoryName { get; set; }

        public int? AccountID { get; set; }
        public Account? Account { get; set; }

        public string? Name { get; set; }
        public int BudgetedPercentage { get; set; }
        public int BudgetedAmount { get; set; }
        public int CurrentBalance { get; set; }

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
}