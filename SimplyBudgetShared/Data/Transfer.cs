﻿using System;
using System.Threading.Tasks;
using SQLite;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudgetShared.Data
{
    public class Transfer : BaseItem
    {
        public int FromExpenseCategoryID { get; set; }
        public int ToExpenseCategoryID { get; set; }
        public string Description { get; set; }

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

        private DateTime _date;
        [Indexed]
        public DateTime Date
        {
            get { return _date; }
            set { _date = value.Date; } //Ensure that we only capture the date
        }

        protected override async Task Create()
        {
            await base.Create();
            var fromExpenseCategory = await GetConnection().GetAsync<ExpenseCategory>(FromExpenseCategoryID);
            var toExpenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ToExpenseCategoryID);
            if (fromExpenseCategory != null && toExpenseCategory != null)
            {
                fromExpenseCategory.CurrentBalance -= Amount;
                toExpenseCategory.CurrentBalance += Amount;
                await fromExpenseCategory.Save();
                await toExpenseCategory.Save();
            }
            NotificationCenter.PostEvent(new TransferEvent(this, EventType.Created));
        }

        protected override async Task Update()
        {
            await base.Update();
            var fromExpenseCategory = await GetConnection().GetAsync<ExpenseCategory>(FromExpenseCategoryID);
            var toExpenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ToExpenseCategoryID);
            if (fromExpenseCategory != null && toExpenseCategory != null && _ammount.Modified)
            {
                fromExpenseCategory.CurrentBalance += _ammount.OriginalValue;
                fromExpenseCategory.CurrentBalance -= _ammount.Value;
                toExpenseCategory.CurrentBalance -= _ammount.OriginalValue;
                toExpenseCategory.CurrentBalance += _ammount.Value;
                await fromExpenseCategory.Save();
                await toExpenseCategory.Save();
                _ammount.Saved();
            }
            NotificationCenter.PostEvent(new TransferEvent(this, EventType.Updated));
        }

        public override async Task Delete()
        {
            await base.Delete();

            var fromExpenseCategory = await GetConnection().GetAsync<ExpenseCategory>(FromExpenseCategoryID);
            var toExpenseCategory = await GetConnection().GetAsync<ExpenseCategory>(ToExpenseCategoryID);
            if (fromExpenseCategory != null && toExpenseCategory != null)
            {
                fromExpenseCategory.CurrentBalance += Amount;
                toExpenseCategory.CurrentBalance -= Amount;
                await fromExpenseCategory.Save();
                await toExpenseCategory.Save();
            }
            NotificationCenter.PostEvent(new TransferEvent(this, EventType.Deleted));
        }
    }
}