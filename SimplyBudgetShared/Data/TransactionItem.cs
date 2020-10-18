using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("TransactionItem")]
    public class TransactionItem : BaseItem, IBeforeCreate, IBeforeRemove
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
    }
}