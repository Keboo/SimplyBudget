using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class ExpenseCategoryEvent : Event
    {
        public ExpenseCategoryEvent(ExpenseCategory expenseCategory, EventType type)
        {
            if (expenseCategory is null) throw new ArgumentNullException(nameof(expenseCategory));
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            ExpenseCategory = expenseCategory;
            Type = type;
        }

        public ExpenseCategory ExpenseCategory { get; }

        public EventType Type { get; }
    }
}