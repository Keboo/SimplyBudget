using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudgetShared.Data
{
    public class Account : BaseItem
    {
        public string? Name { get; set; }

        private DateTime _validatedDate;
        public DateTime ValidatedDate
        {
            get => _validatedDate;
            set => _validatedDate = value.Date;  //Ensure we only capture the date
        }

        //[Indexed]
        public bool IsDefault { get; set; }

        public async Task<int> GetCurrentAmount()
        {
            var expenseCategories = await GetExpenseCategories();
            return expenseCategories.Sum(x => x.CurrentBalance);
        }

        public async Task<IList<ExpenseCategory>> GetExpenseCategories()
        {
            return null!;
            //return await GetConnection().Table<ExpenseCategory>().Where(x => x.AccountID == ID).ToListAsync();
        }

        protected override async Task Create()
        {
            await base.Create();
            if (IsDefault)
            {
                await ClearCurrentDefault();
            }
            NotificationCenter.PostEvent(new AccountEvent(this, EventType.Created));
        }

        protected override async Task Update()
        {
            await base.Update();
            if (IsDefault)
            {
                await ClearCurrentDefault();
            }
            NotificationCenter.PostEvent(new AccountEvent(this, EventType.Updated));
        }

        public override async Task Delete()
        {
            await base.Delete();
            if (IsDefault)
            {
                //Select the first account to be the new default
                //var firstAccount = await GetConnection().Table<Account>().FirstOrDefaultAsync();
                //if (firstAccount != null)
                //{
                //    firstAccount.IsDefault = true;
                //    await firstAccount.Save();
                //}
            }
            NotificationCenter.PostEvent(new AccountEvent(this, EventType.Deleted));
        }

        private async Task ClearCurrentDefault()
        {
            //Clear any other defaults
            var currentDefault = await GetDefault();
            if (currentDefault != null && currentDefault.ID != ID)
            {
                currentDefault.IsDefault = false;
                await currentDefault.Save();
            }
        }

        public static async Task<Account> GetDefault()
        {
            return null!;
            //return await DatabaseManager.Instance.Connection.Table<Account>().Where(x => x.IsDefault).FirstOrDefaultAsync();
        }
    }
}