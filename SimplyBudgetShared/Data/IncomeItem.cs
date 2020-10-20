
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("IncomeItem")]
    public class IncomeItem : BaseItem, IBeforeCreate, IBeforeRemove, IEquatable<IncomeItem?>
    {
        //[Indexed]
        public int IncomeID { get; set; }
        public Income? Income { get; set; }

        //[Indexed]
        public int ExpenseCategoryID { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        public int Amount { get; set; }

        public string? Description { get; set; }

        public async Task BeforeCreate(BudgetContext context)
        {
            var category = await context.FindAsync<ExpenseCategory>(ExpenseCategoryID);
            category.CurrentBalance += Amount;
        }

        public async Task BeforeRemove(BudgetContext context)
        {
            var category = await context.FindAsync<ExpenseCategory>(ExpenseCategoryID);
            category.CurrentBalance -= Amount;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as IncomeItem);
        }

        public bool Equals(IncomeItem? other)
        {
            return other != null &&
                   base.Equals(other) &&
                   IncomeID == other.IncomeID &&
                   ExpenseCategoryID == other.ExpenseCategoryID &&
                   Amount == other.Amount &&
                   Description == other.Description;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), IncomeID, ExpenseCategoryID, Amount, Description);
        }

        public static bool operator ==(IncomeItem? left, IncomeItem? right)
        {
            return EqualityComparer<IncomeItem>.Default.Equals(left, right);
        }

        public static bool operator !=(IncomeItem? left, IncomeItem? right)
        {
            return !(left == right);
        }
    }
}
