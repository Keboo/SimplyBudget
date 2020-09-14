using System.Collections.Generic;
using SimplyBudgetShared.Data;
using SQLite;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Telerik.JustMock;

namespace SimplyBudgetSharedTests.Data
{
    public static class CommonActions
    {
        public static SQLiteAsyncConnection MockConnection()
        {
            var manager = Mock.Create<DatabaseManager>(Constructor.Mocked, Behavior.Strict);
            Mock.SetupStatic<DatabaseManager>(Behavior.Strict);
            Mock.Arrange(() => DatabaseManager.Instance).Returns(manager);
            var connection = Mock.Create<SQLiteAsyncConnection>(Behavior.Strict);
            Mock.Arrange(() => manager.Connection).Returns(connection);

            return connection;
        }

        public static List<T> MockQuery<T>(this SQLiteAsyncConnection connection) where T : new()
        {
            var rv = new List<T>();
            var tableQuery = Mock.Create<AsyncTableQuery<T>>(Behavior.Strict);
            Mock.Arrange(() => connection.Table<T>()).Returns(tableQuery);
            Mock.Arrange(() => tableQuery.Where(Arg.IsAny<Expression<Func<T, bool>>>())).Returns(tableQuery);
            Mock.Arrange(() => tableQuery.ToListAsync()).Returns(Task.FromResult(rv));
            return rv;
        }
    }
}