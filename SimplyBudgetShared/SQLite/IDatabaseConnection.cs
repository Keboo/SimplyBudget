using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SimplyBudgetShared.SQLite
{
    public interface IDatabaseConnection
    {
        Task<int> InsertAsync(object item);
        Task<int> DeleteAsync(object item);
        Task<int> UpdateAsync(object item);
        Task<T> GetAsync<T>(object pk) where T : new();

        Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new();

        IQuery<T> Table<T>() where T : new();
        Task<bool> CreateTablesAsync(params Type[] types);
    }

    public interface IQuery<T> where T : new()
    {
        IQuery<T> Where(Expression<Func<T, bool>> predExpr);
        IQuery<T> Skip(int n);
        IQuery<T> Take(int n);
        IQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr);
        IQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr);

        Task<List<T>> ToListAsync();
        
        Task<int> CountAsync();

        Task<T> ElementAtAsync(int index);
        Task<T> FirstAsync();
        Task<T> FirstOrDefaultAsync();
    
    }
}