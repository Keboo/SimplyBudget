using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class IncomeEvent : Event
    {
        private readonly Income _income;
        private readonly EventType _type;

        public IncomeEvent(Income income, EventType type)
        {
            if (income == null) throw new ArgumentNullException("income");
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", "type");
            _income = income;
            _type = type;
        }

        public Income Income
        {
            get { return _income; }
        }

        public EventType Type
        {
            get { return _type; }
        }
    }
}