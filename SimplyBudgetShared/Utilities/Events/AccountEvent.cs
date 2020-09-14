using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class AccountEvent : Event
    {
        private readonly Account _acount;
        private readonly EventType _type;

        public AccountEvent(Account account, EventType type)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", "type");
            _acount = account;
            _type = type;
        }

        public Account Account
        {
            get { return _acount; }
        }

        public EventType Type
        {
            get { return _type; }
        }
    }
}