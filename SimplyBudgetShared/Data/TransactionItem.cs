﻿using System.Threading.Tasks;
using SQLite;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudgetShared.Data
{
    public class TransactionItem : BaseItem
    {
        [Indexed]
        public int TransactionID { get; set; }

        [Indexed]
        public int ExpenseCategoryID { get; set; }

        private DBProp<int> _ammount; 
        public int Amount
        {
            get { return _ammount; }
            set
            {
                if (_ammount == null)
                    _ammount = new DBProp<int>(value);
                else
                    _ammount.Value = value;
            }
        }

        public string Description { get; set; }

        public override async Task Delete()
        {
            await base.Delete();
            NotificationCenter.PostEvent(new TransactionItemEvent(this, EventType.Deleted));

            var expenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ExpenseCategoryID);
            if (expenseCategory != null)
            {
                expenseCategory.CurrentBalance += Amount;
                await expenseCategory.Save();
            }
        }

        protected override async Task Create()
        {
            await base.Create();
            NotificationCenter.PostEvent(new TransactionItemEvent(this, EventType.Created));

            var expenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ExpenseCategoryID);
            if (expenseCategory != null)
            {
                expenseCategory.CurrentBalance -= Amount;
                await expenseCategory.Save();
            }
        }

        protected override async Task Update()
        {
            await base.Update();
            NotificationCenter.PostEvent(new TransactionItemEvent(this, EventType.Updated));

            if (_ammount != null && _ammount.Modified)
            {
                var expenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ExpenseCategoryID);
                if (expenseCategory != null)
                {
                    expenseCategory.CurrentBalance += _ammount.OriginalValue;
                    expenseCategory.CurrentBalance -= _ammount.Value;
                    await expenseCategory.Save();
                    _ammount.Saved();
                }   
            }
        }
    }
}