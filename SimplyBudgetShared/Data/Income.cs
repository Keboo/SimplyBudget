using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data
{
    [Table("Income")]
    public class Income : BaseItem
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

        public List<IncomeItem>? IncomeItems { get; set; }
    }
}
