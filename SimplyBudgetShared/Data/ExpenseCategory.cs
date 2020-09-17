

using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public class ExpenseCategory : BaseItem
    {
        public string? CategoryName { get; set; }

        //[Indexed]
        public int AccountID { get; set; }
        public Account? Account { get; set; }

        public string? Name { get; set; }
        public int BudgetedPercentage { get; set; }
        public int BudgetedAmount { get; set; }
        public int CurrentBalance { get; set; }

        public bool UsePercentage
        {
            get { return BudgetedPercentage > 0; }
        }

        public async Task<IList<IncomeItem>> GetIncomeItems(DateTime? queryStart = null, DateTime? queryEnd = null)
        {
            return default!;
            //if (queryStart != null && queryEnd != null)
            //{
            //    var transactions = await GetConnection().Table<Income>().Where(x => x.Date >= queryStart && x.Date <= queryEnd).ToListAsync();
            //    var rv = new List<IncomeItem>();
            //    foreach (var transaction in transactions)
            //        rv.AddRange((await transaction.GetIncomeItems()).Where(x => x.ExpenseCategoryID == ID));
            //    return rv;
            //}
            //return await GetConnection().Table<IncomeItem>().Where(
            //    x => x.ExpenseCategoryID == ID).ToListAsync();
        }

        public string GetBudgetedDisplayString()
        {
            return UsePercentage
                       ? BudgetedPercentage.FormatPercentage()
                       : BudgetedAmount.FormatCurrency();
        }

        public override async Task Delete()
        {
            await base.Delete();
            NotificationCenter.PostEvent(new ExpenseCategoryEvent(this, EventType.Deleted));
        }

        protected override async Task Create()
        {
            if (AccountID == 0)
            {
                var defaultAccount = await Account.GetDefault();
                if (defaultAccount != null)
                    AccountID = defaultAccount.ID;
            }

            await base.Create();
            NotificationCenter.PostEvent(new ExpenseCategoryEvent(this, EventType.Created));
        }

        protected override async Task Update()
        {
            await base.Update();
            NotificationCenter.PostEvent(new ExpenseCategoryEvent(this, EventType.Updated));
        }
    }
}