using SimplyBudget.Core.Data;

namespace SimplyBudget.Core.Import;

public interface IImport
{
    IAsyncEnumerable<ExpenseCategoryItem> GetItems();
}
