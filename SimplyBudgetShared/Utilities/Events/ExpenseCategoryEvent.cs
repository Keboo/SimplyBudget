using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class ExpenseCategoryEvent : Event
    {
        private readonly ExpenseCategory _expenseCategory;
        private readonly EventType _type;

        public ExpenseCategoryEvent(ExpenseCategory expenseCategory, EventType type)
        {
            if (expenseCategory == null) throw new ArgumentNullException("expenseCategory");
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", "type");
            _expenseCategory = expenseCategory;
            _type = type;
        }

        public ExpenseCategory ExpenseCategory
        {
            get { return _expenseCategory; }
        }

        public EventType Type
        {
            get { return _type; }
        }
    }
}