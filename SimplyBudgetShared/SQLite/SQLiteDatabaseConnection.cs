using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.SQLite
{
    public class SQLiteDatabaseConnection : IDatabaseConnection
    {
        private readonly SQLiteAsyncConnection _connection;

        public SQLiteDatabaseConnection(string databasePath)
        {
            _connection = new SQLiteAsyncConnection(databasePath);
        }

        public Task<int> InsertAsync(object item)
        {
            return _connection.InsertAsync(item);
        }

        public Task<int> DeleteAsync(object item)
        {
            return _connection.DeleteAsync(item);
        }

        public Task<int> UpdateAsync(object item)
        {
            return _connection.UpdateAsync(item);
        }

        public Task<T> GetAsync<T>(object pk) where T : new()
        {
            return _connection.GetAsync<T>(pk);
        }

        public Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
        {
            return _connection.QueryAsync<T>(sql, args);
        }

        public IQuery<T> Table<T>() where T : new()
        {
            return new SQLiteQuery<T>(_connection.Table<T>());
        }

        public async Task<bool> CreateTablesAsync(params Type[] types)
        {
            var rv = await _connection.CreateTablesAsync(types);

            return rv.Results.Values.All(x => x == 0);
        }
    }
}