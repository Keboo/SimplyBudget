using System;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Utilities.Events
{
    public abstract class DatabaseEvent : Event
    {
        protected DatabaseEvent(BudgetContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public BudgetContext Context { get; }
    }
}