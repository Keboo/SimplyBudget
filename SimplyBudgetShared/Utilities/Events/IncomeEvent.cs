using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class IncomeEvent : Event
    {
        public IncomeEvent(Income income, EventType type)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Income = income ?? throw new ArgumentNullException(nameof(income));
            Type = type;
        }

        public Income Income { get; }

        public EventType Type { get; }
    }
}