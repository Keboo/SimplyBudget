using Microsoft.EntityFrameworkCore;
using SimplyBudget.Utilities;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;
using System;
using System.Collections.ObjectModel;

namespace SimplyBudget.Collections
{
    public class ExpenseCategoryCollection : ObservableCollection<ExpenseCategory>, IEventListener<ExpenseCategoryEvent>
    {
        private static readonly Lazy<ExpenseCategoryCollection> _instance = new Lazy<ExpenseCategoryCollection>(() => new ExpenseCategoryCollection());

        public static ExpenseCategoryCollection Instance => _instance.Value;

        private BudgetContext Context { get; } = BudgetContext.Instance;

        private ExpenseCategoryCollection()
        {
            if (DesignerHelper.IsDesignMode == false)
            {
                ReloadItems();
                NotificationCenter.Register(this);
            }
        }

        private async void ReloadItems()
        {
            var expenseCategories = await Context.ExpenseCategories.ToListAsync();
            Clear();
            foreach(var item in expenseCategories)
                Add(item);
        }

        public void HandleEvent(ExpenseCategoryEvent @event)
        {
            ReloadItems();
        }
    }
}