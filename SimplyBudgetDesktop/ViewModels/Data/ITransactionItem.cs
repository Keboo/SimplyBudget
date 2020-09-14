using System;

namespace SimplyBudget.ViewModels.Data
{
    public interface ITransactionItem : IDatabaseItem
    {
        string ExpenseCategoryName { get; }
        string Description { get; }
        int Amount { get; }
        DateTime Date { get; }
    }
}