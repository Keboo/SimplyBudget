using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("Transaction")]
    public class Transaction : BaseItem
    {
        private DateTime _date;
        //[Indexed]
        public DateTime Date
        {
            get => _date;
            set => _date = value.Date;  //Ensure that we only capture the date
        }

        public string? Description { get; set; }

        public async Task<IList<TransactionItem>> GetTransactionItems()
        {
            return default!;
            //return await GetConnection().Table<TransactionItem>().Where(x => x.TransactionID == ID).ToListAsync();
        }

        public override async Task Delete()
        {
            await base.Delete();

            //foreach (var item in await GetTransactionItems())
            //    await item.Delete();

        }
    }
}