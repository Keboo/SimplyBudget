using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Events
{
    public class IncomeEvent : DatabaseEvent
    {
        public IncomeEvent(BudgetContext context, Income income, EventType type)
            : base(context)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Income = income ?? throw new ArgumentNullException(nameof(income));
            Type = type;
        }

        public Income Income { get; }

        public EventType Type { get; }
    }
}