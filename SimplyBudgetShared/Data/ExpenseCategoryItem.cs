using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyBudgetShared.Data;

[Table("ExpenseCategoryItem")]
public class ExpenseCategoryItem : BaseItem, IBeforeRemove
{
    private DateTime _date;
    public DateTime Date
    {
        get => _date;
        set => _date = value.Date;  //Ensure that we only capture the date
    }

    public string? Description { get; set; }

    public List<ExpenseCategoryItemDetail>? Details { get; set; }

    public async Task BeforeRemove(BudgetContext context)
    {
        await foreach (var item in context.ExpenseCategoryItemDetails.Where(x => x.ExpenseCategoryItemId == ID).AsAsyncEnumerable())
        {
            context.Remove(item);
        }
    }

    public bool IsTransfer =>
        Details?.Count == 2 &&
        Details[0].Amount + Details[1].Amount == 0;

    [NotMapped]
    public bool? IgnoreBudget
    {
        get
        {
            bool? result = null;
            foreach(var detail in Details ?? Enumerable.Empty<ExpenseCategoryItemDetail>())
            {
                if (result is null)
                {
                    result = detail.IgnoreBudget;
                }
                else if (result != detail.IgnoreBudget)
                {
                    //Mismatching items, return null
                    return null;
                }
            }
            return result;
        }
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            foreach(var detail in Details ?? Enumerable.Empty<ExpenseCategoryItemDetail>())
            {
                detail.IgnoreBudget = value.Value;
            }
        }
    }
}