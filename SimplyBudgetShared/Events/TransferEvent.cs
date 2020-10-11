using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Events
{
    public class TransferEvent : DatabaseEvent
    {
        public TransferEvent(BudgetContext context, Transfer transfer, EventType type)
            : base(context)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Transaction = transfer ?? throw new ArgumentNullException(nameof(transfer));
            Type = type;
        }

        public Transfer Transaction { get; }

        public EventType Type { get; }
    }
}