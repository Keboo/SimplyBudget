using System.Threading.Tasks;

namespace SimplyBudgetShared.Data;

internal interface IBeforeCreate
{
    Task BeforeCreate(BudgetContext context);
}
