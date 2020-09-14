using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class TransferEvent : Event
    {
        public TransferEvent(Transfer transfer, EventType type)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Transaction = transfer ?? throw new ArgumentNullException(nameof(transfer));
            Type = type;
        }

        public Transfer Transaction { get; }

        public EventType Type { get; }
    }
}