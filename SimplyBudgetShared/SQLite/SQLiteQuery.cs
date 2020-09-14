using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SQLite;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.SQLite
{
    public class SQLiteQuery<T> : IQuery<T> where T : new()
    {
        private readonly AsyncTableQuery<T> _tableQuery;
 
        internal SQLiteQuery([NotNull] AsyncTableQuery<T> tableQuery)
        {
            if (tableQuery == null) throw new ArgumentNullException("tableQuery");
            _tableQuery = tableQuery;
        }

        public IQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            return new SQLiteQuery<T>(_tableQuery.Where(predExpr));
        }

        public IQuery<T> Skip(int n)
        {
            return new SQLiteQuery<T>(_tableQuery.Skip(n));
        }

        public IQuery<T> Take(int n)
        {
            return new SQLiteQuery<T>(_tableQuery.Take(n));
        }

        public IQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr)
        {
            return new SQLiteQuery<T>(_tableQuery.OrderBy(orderExpr));
        }

        public IQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr)
        {
            return new SQLiteQuery<T>(_tableQuery.OrderByDescending(orderExpr));
        }

        public Task<List<T>> ToListAsync()
        {
            return _tableQuery.ToListAsync();
        }

        public Task<int> CountAsync()
        {
            return _tableQuery.CountAsync();
        }

        public Task<T> ElementAtAsync(int index)
        {
            return _tableQuery.ElementAtAsync(index);
        }

        public Task<T> FirstAsync()
        {
            return _tableQuery.FirstAsync();
        }

        public Task<T> FirstOrDefaultAsync()
        {
            return _tableQuery.FirstOrDefaultAsync();
        }
    }
}