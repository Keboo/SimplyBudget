using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class TransactionEvent : Event
    {
        private readonly Transaction _transaction;
        private readonly EventType _type;

        public TransactionEvent(Transaction transaction, EventType type)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", "type");
            _transaction = transaction;
            _type = type;
        }

        public Transaction Transaction
        {
            get { return _transaction; }
        }

        public EventType Type
        {
            get { return _type; }
        } 
    }
}