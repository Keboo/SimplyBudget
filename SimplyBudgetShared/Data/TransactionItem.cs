using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("TransactionItem")]
    public class TransactionItem : BaseItem, IBeforeCreate
    {
        public int TransactionID { get; set; }

        public int ExpenseCategoryID { get; set; }

        public int Amount { get; set; }

        public string? Description { get; set; }

        public async Task BeforeCreate(BudgetContext context)
        {
            var category = await context.FindAsync<ExpenseCategory>(ExpenseCategoryID);
            category.CurrentBalance -= Amount;
        }
    }
}