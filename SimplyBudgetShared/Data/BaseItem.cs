using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public abstract class BaseItem
    {
        //[PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is BaseItem other)
                return Equals(other);
            return false;
        }

        protected bool Equals(BaseItem other)
        {
            return ID == other.ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }

        //protected SQLiteAsyncConnection GetConnection()
        //{
        //    return DatabaseManager.Instance.Connection;
        //}

        public virtual async Task Delete()
        {
            //await GetConnection().DeleteAsync(this);
        }

        public async Task<BaseItem> Save()
        {
            //if (ID == 0)
            //    await Create();
            //else
            //    await Update();
            return this;
        }

        protected virtual async Task Create()
        {
            //await GetConnection().InsertAsync(this);
        }

        protected virtual async Task Update()
        {
            //await GetConnection().UpdateAsync(this);
        }
    }

}