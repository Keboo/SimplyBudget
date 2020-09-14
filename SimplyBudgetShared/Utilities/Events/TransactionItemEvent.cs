using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class TransactionItemEvent : Event
    {
        public TransactionItemEvent(TransactionItem transactionItem, EventType type)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            TransactionItem = transactionItem ?? throw new ArgumentNullException(nameof(transactionItem));
            Type = type;
        }

        public TransactionItem TransactionItem { get; }

        public EventType Type { get; }
    }
}