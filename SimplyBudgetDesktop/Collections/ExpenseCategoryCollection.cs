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

        public static ExpenseCategoryCollection Instance
        {
            get { return _instance.Value; }
        }

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
            var expenseCategories = await DatabaseManager.Instance.Connection.Table<ExpenseCategory>().ToListAsync();
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