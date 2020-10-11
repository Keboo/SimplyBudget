using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Events
{
    public class IncomeItemEvent : DatabaseEvent
    {
        public IncomeItemEvent(BudgetContext context, IncomeItem incomeItem, EventType type)
            : base(context)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            IncomeItem = incomeItem ?? throw new ArgumentNullException(nameof(incomeItem));
            Type = type;
        }

        public IncomeItem IncomeItem { get; }

        public EventType Type { get; }
    }
}