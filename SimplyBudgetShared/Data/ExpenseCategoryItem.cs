using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("ExpenseCategoryItem")]
    public class ExpenseCategoryItem : BaseItem, IBeforeRemove
    {
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
            await foreach (var item in context.ExpenseCategoryItemDetails.Where(x => x.ExpenseCategoryItemId == ID).AsAsyncEnumerable())
            {
                context.Remove(item);
            }
        }
    }
}