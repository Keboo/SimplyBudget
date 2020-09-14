using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class IncomeItemEvent : Event
    {
        public IncomeItemEvent(IncomeItem incomeItem, EventType type)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            IncomeItem = incomeItem ?? throw new ArgumentNullException(nameof(incomeItem));
            Type = type;
        }

        public IncomeItem IncomeItem { get; }

        public EventType Type { get; }
    }
}