using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("Transfer")]
    public class Transfer : BaseItem, IBeforeCreate, IBeforeRemove
    {
        public int FromExpenseCategoryID { get; set; }
        public int ToExpenseCategoryID { get; set; }
        public string? Description { get; set; }

        public int Amount { get; set; }

        private DateTime _date;
        //[Indexed]
        public DateTime Date
        {
            get => _date;
            set => _date = value.Date;  //Ensure that we only capture the date
        }

        public async Task BeforeCreate(BudgetContext context)
        {
            var fromExpenseCategory = await context.FindAsync<ExpenseCategory>(FromExpenseCategoryID);
            var toExpenseCategory = await context.FindAsync<ExpenseCategory>(ToExpenseCategoryID);
            if (fromExpenseCategory != null && toExpenseCategory != null)
            {
                fromExpenseCategory.CurrentBalance -= Amount;
                toExpenseCategory.CurrentBalance += Amount;
            }
            await context.SaveChangesAsync();
        }

        public async Task BeforeRemove(BudgetContext context)
        {
            var fromExpenseCategory = await context.FindAsync<ExpenseCategory>(FromExpenseCategoryID);
            var toExpenseCategory = await context.FindAsync<ExpenseCategory>(ToExpenseCategoryID);
            if (fromExpenseCategory != null && toExpenseCategory != null)
            {
                fromExpenseCategory.CurrentBalance += Amount;
                toExpenseCategory.CurrentBalance -= Amount;
            }
            await context.SaveChangesAsync();
        }
    }
}