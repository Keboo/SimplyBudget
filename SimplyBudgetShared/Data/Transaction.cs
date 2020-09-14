using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudgetShared.Data
{
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

        public async Task<TransactionItem> AddTransactionItem(int expenseCategoryID, int amount, string description)
        {
            if (ID == 0) return null;
            if (expenseCategoryID == 0) return null;
            var transactionItem = new TransactionItem
                                      {
                                          Amount = amount,
                                          Description = Description,
                                          ExpenseCategoryID = expenseCategoryID,
                                          TransactionID = ID
                                      };
            await transactionItem.Save();
            return transactionItem;
        }

        public override async Task Delete()
        {
            await base.Delete();

            foreach (var item in await GetTransactionItems())
                await item.Delete();

            NotificationCenter.PostEvent(new TransactionEvent(this, EventType.Deleted));
        }

        protected override async Task Create()
        {
            await base.Create();
            NotificationCenter.PostEvent(new TransactionEvent(this, EventType.Created));
        }

        protected override async Task Update()
        {
            await base.Update();
            NotificationCenter.PostEvent(new TransactionEvent(this, EventType.Updated));
        }
    }
}