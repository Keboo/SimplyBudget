using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class TransactionItemEvent : Event
    {
        private readonly TransactionItem _transactionItem;
        private readonly EventType _type;

        public TransactionItemEvent(TransactionItem transactionItem, EventType type)
        {
            if (transactionItem == null) throw new ArgumentNullException("transactionItem");
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", "type");
            _transactionItem = transactionItem;
            _type = type;
        }

        public TransactionItem TransactionItem
        {
            get { return _transactionItem; }
        }

        public EventType Type
        {
            get { return _type; }
        }
    }
}