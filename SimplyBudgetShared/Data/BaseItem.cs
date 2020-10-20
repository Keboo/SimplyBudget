namespace SimplyBudgetShared.Data
{
    public abstract class BaseItem
    {
        public int ID { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is BaseItem other)
                return Equals(other);
            return false;
        }

        protected virtual bool Equals(BaseItem other)
        {
            return ID == other?.ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }
    }
}