using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class AccountEvent : Event
    {
        public AccountEvent(Account account, EventType type)
        {
            if (account is null) throw new ArgumentNullException(nameof(account));
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Account = account;
            Type = type;
        }

        public Account Account { get; }

        public EventType Type { get; }
    }
}