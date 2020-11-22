using System;
using System.Collections.Generic;

namespace SimplyBudgetShared.Data
{
    public class Transaction : BaseItem
    {
        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => _date = value.Date;
        }

        public List<TransactionItem>? TransactionItems { get; set; }

        public string? Description { get; set; }
    }
}