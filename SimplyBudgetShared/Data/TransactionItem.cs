using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data
{
    [Table("TransactionItem")]
    public class TransactionItem : BaseItem
    {
        public int TransactionID { get; set; }

        public int ExpenseCategoryID { get; set; }

        public int Amount { get; set; }

        public string? Description { get; set; }
    }
}