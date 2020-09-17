
using System.Threading.Tasks;

using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudgetShared.Data
{
    public class IncomeItem : BaseItem
    {
        //[Indexed]
        public int IncomeID { get; set; }

        //[Indexed]
        public int ExpenseCategoryID { get; set; }

        private DBProp<int> _ammount;
        public int Amount
        {
            get => _ammount;
            set
            {
                if (_ammount is null)
                    _ammount = new DBProp<int>(value);
                else
                    _ammount.Value = value;
            }
        }

        public string? Description { get; set; }

        public override async Task Delete()
        {
            await base.Delete();
            NotificationCenter.PostEvent(new IncomeItemEvent(this, EventType.Deleted));

            //var expenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ExpenseCategoryID);
            //if (expenseCategory != null)
            //{
            //    expenseCategory.CurrentBalance -= Amount;
            //    await expenseCategory.Save();
            //}
        }

        protected override async Task Create()
        {
            await base.Create();
            NotificationCenter.PostEvent(new IncomeItemEvent(this, EventType.Created));

            //var expenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ExpenseCategoryID);
            //if (expenseCategory != null)
            //{
            //    expenseCategory.CurrentBalance += Amount;
            //    await expenseCategory.Save();
            //}
        }

        protected override async Task Update()
        {
            await base.Update();
            NotificationCenter.PostEvent(new IncomeItemEvent(this, EventType.Updated));


            if (_ammount != null && _ammount.Modified)
            {
                //var expenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ExpenseCategoryID);
                //if (expenseCategory != null)
                //{
                //    expenseCategory.CurrentBalance -= _ammount.OriginalValue;
                //    expenseCategory.CurrentBalance += _ammount.Value;
                //    await expenseCategory.Save();
                //}
            }
        }
    }
}
