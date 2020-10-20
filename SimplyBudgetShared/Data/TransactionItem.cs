using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("TransactionItem")]
    public class TransactionItem : BaseItem, IBeforeCreate, IBeforeRemove, IEquatable<TransactionItem?>
    {
        public int TransactionID { get; set; }
        public Transaction? Transaction { get; set; }

        public int ExpenseCategoryID { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        public int Amount { get; set; }

        public string? Description { get; set; }

        public async Task BeforeCreate(BudgetContext context)
        {
            var category = await context.FindAsync<ExpenseCategory>(ExpenseCategoryID);
            category.CurrentBalance -= Amount;
        }

        public async Task BeforeRemove(BudgetContext context)
        {
            var category = await context.FindAsync<ExpenseCategory>(ExpenseCategoryID);
            category.CurrentBalance += Amount;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as TransactionItem);
        }

        public bool Equals(TransactionItem? other)
        {
            return other != null &&
                   base.Equals(other) &&
                   TransactionID == other.TransactionID &&
                   ExpenseCategoryID == other.ExpenseCategoryID &&
                   Amount == other.Amount &&
                   Description == other.Description;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), TransactionID, ExpenseCategoryID, Amount, Description);
        }

        public static bool operator ==(TransactionItem? left, TransactionItem? right)
        {
            return EqualityComparer<TransactionItem>.Default.Equals(left, right);
        }

        public static bool operator !=(TransactionItem? left, TransactionItem? right)
        {
            return !(left == right);
        }
    }
}