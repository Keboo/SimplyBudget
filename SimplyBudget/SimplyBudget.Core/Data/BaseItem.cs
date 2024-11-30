namespace SimplyBudget.Core.Data;

public abstract class BaseItem
{
    public int ID { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is BaseItem other)
        {
            return Equals(other);
        }
        return false;
    }

    protected virtual bool Equals(BaseItem other)
    {
        if (ID == 0)
        {
            return ReferenceEquals(this, other);
        }
        return 
            GetType() == other.GetType() &&
            ID == other?.ID;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), ID);
    }
}