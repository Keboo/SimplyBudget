using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public class AccountEvent : DatabaseEvent
    {
        public AccountEvent(BudgetContext context, Account account, EventType type)
            : base(context)
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