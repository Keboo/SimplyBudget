
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

        public async Task<IList<IncomeItem>> GetIncomeItems()
        {
            return default!;
            //return await GetConnection().Table<IncomeItem>().Where(x => x.IncomeID == ID).ToListAsync();
        }

        public async Task<IncomeItem> AddIncomeItem(int expenseCategoryID, int amount)
        {
            if (ID == 0) return null;
            if (expenseCategoryID == 0) return null;
            var incomeItem = new IncomeItem
            {
                Amount = amount,
                Description = Description,
                ExpenseCategoryID = expenseCategoryID,
                IncomeID = ID
            };
            await incomeItem.Save();
            return incomeItem;
        }

        public override async Task Delete()
        {
            await base.Delete();
            foreach (var item in await GetIncomeItems())
                await item.Delete();

            //NotificationCenter.PostEvent(new IncomeEvent(this, EventType.Deleted));
        }

        public async Task BeforeRemove(BudgetContext context)
        {
            await foreach(var item in context.IncomeItems.Where(x => x.IncomeID == ID).AsAsyncEnumerable())
            {
                context.Remove(item);
            }
        }

        //protected override async Task Create()
        //{
        //    await base.Create();
        //    NotificationCenter.PostEvent(new IncomeEvent(this, EventType.Created));
        //}

        //protected override async Task Update()
        //{
        //    await base.Update();
        //    NotificationCenter.PostEvent(new IncomeEvent(this, EventType.Updated));
        //}
    }
}
