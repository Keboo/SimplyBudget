using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SimplyBudgetShared.Data
{
    [Table("ExpenseCategoryItemDetail")]
    public class ExpenseCategoryItemDetail : BaseItem
    {
        public int ExpenseCategoryItemId { get; set; }
        public ExpenseCategoryItem? ExpenseCategoryItem { get; set; }

        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        public int Amount { get; set; }

        public bool Equals([AllowNull] ExpenseCategoryItemDetail other)
        {
            return ExpenseCategoryItemId == other?.ExpenseCategoryItemId;
        }
    }
}