using SimplyBudgetShared.Data;
using System;

namespace SimplyBudgetShared.Events
{
    public class DatabaseEvent<T> : DatabaseEvent 
        where T : BaseItem
    {
        public DatabaseEvent(BudgetContext context, T item, EventType type)
            : base(context)
        {
            if (type == EventType.None) throw new ArgumentException(@"A type must be specified", nameof(type));
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Type = type;
        }

        public T Item { get; }

        public EventType Type { get; }
    }

    public abstract class DatabaseEvent
    {
        protected DatabaseEvent(BudgetContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public BudgetContext Context { get; }
    }
}