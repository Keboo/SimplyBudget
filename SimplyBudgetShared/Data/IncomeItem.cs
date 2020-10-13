
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data
{
    [Table("IncomeItem")]
    public class IncomeItem : BaseItem
    {
        //[Indexed]
        public int IncomeID { get; set; }

        //[Indexed]
        public int ExpenseCategoryID { get; set; }

        public int Amount { get; set; }

        public string? Description { get; set; }
    }
}
