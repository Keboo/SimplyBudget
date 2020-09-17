using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{

    public class ExpenseCategoryEvent : DatabaseEvent
    {
        public ExpenseCategoryEvent(BudgetContext context, ExpenseCategory expenseCategory, EventType type)
            : base(context)
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