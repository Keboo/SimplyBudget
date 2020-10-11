using SimplyBudgetShared.Data;
using System;

namespace SimplyBudgetShared.Events
{
    public abstract class DatabaseEvent
    {
        protected DatabaseEvent(BudgetContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public BudgetContext Context { get; }
    }
}