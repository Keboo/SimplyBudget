using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    internal interface IBeforeRemove
    {
        Task BeforeRemove(BudgetContext context);
    }
}
