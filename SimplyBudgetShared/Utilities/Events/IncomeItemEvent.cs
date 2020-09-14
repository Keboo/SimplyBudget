using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class IncomeItemEvent : Event
    {
        private readonly IncomeItem _incomeItem;
        private readonly EventType _type;

        public IncomeItemEvent(IncomeItem incomeItem, EventType type)
        {
            if (incomeItem == null) throw new ArgumentNullException("incomeItem");
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", "type");
            _incomeItem = incomeItem;
            _type = type;
        }

        public IncomeItem IncomeItem
        {
            get { return _incomeItem; }
        }

        public EventType Type
        {
            get { return _type; }
        }          
    }
}