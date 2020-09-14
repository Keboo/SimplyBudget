using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class TransferEvent : Event
    {
        private readonly Transfer _transfer;
        private readonly EventType _type;

        public TransferEvent(Transfer transfer, EventType type)
        {
            if (transfer == null) throw new ArgumentNullException("transfer");
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", "type");
            _transfer = transfer;
            _type = type;
        }

        public Transfer Transaction
        {
            get { return _transfer; }
        }

        public EventType Type
        {
            get { return _type; }
        }
    }
}