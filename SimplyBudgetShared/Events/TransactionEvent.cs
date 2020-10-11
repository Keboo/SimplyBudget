using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Events
{
    public class TransactionEvent : DatabaseEvent
    {
        public TransactionEvent(BudgetContext context, Transaction transaction, EventType type)
            : base(context)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            Type = type;
        }

        public Transaction Transaction { get; }

        public EventType Type { get; }
    }
}