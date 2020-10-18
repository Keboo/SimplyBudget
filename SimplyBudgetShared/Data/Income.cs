
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("Income")]
    public class Income : BaseItem, IBeforeRemove
    {
        private DateTime _date;
        //[Indexed]
        public DateTime Date
        {
            get => _date;
            set => _date = value.Date;  //Ensure that we only capture the date
        }

        public int TotalAmount { get; set; }

        public string? Description { get; set; }

        public List<IncomeItem>? IncomeItems { get; set; }

        public async Task BeforeRemove(BudgetContext context)
        {
            await foreach(var item in context.IncomeItems.Where(x => x.IncomeID == ID).AsAsyncEnumerable())
            {
                context.Remove(item);
            }
        }
    }
}
