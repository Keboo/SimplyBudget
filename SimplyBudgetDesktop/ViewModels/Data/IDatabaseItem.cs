using SimplyBudgetShared.Data;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.Data
{
    public interface IDatabaseItem
    {
        Task<BaseItem> GetItem();
    }
}