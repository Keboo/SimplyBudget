using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class TransactionEvent : Event
    {
        public TransactionEvent(Transaction transaction, EventType type)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            Type = type;
        }

        public Transaction Transaction { get; }

        public EventType Type { get; }
    }
}