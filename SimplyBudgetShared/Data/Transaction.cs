using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("Transaction")]
    public class Transaction : BaseItem, IBeforeRemove
    {
        private DateTime _date;
        //[Indexed]
        public DateTime Date
        {
            get => _date;
            set => _date = value.Date;  //Ensure that we only capture the date
        }

        public List<TransactionItem>? TransactionItems { get; set; }

        public string? Description { get; set; }

        public async Task BeforeRemove(BudgetContext context)
        {
            await foreach (var item in context.TransactionItems.Where(x => x.TransactionID == ID).AsAsyncEnumerable())
            {
                context.Remove(item);
            }
        }
    }
}