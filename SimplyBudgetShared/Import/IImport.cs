using SimplyBudgetShared.Data;
using System.Collections.Generic;

namespace SimplyBudgetShared.Import;

public interface IImport
{
    IAsyncEnumerable<ExpenseCategoryItem> GetItems();
}
