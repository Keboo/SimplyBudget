using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Events;

public class DatabaseEvent<T> : DatabaseEvent 
    where T : BaseItem
{
    public DatabaseEvent(T item, EventType type)
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
}