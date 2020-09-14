using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;

namespace SimplyBudgetShared.Data
{
    //NB: Schema changes are handled by the SQLite library, this is for data migrations
    public static class DatabaseMigrations
    {
        public static IList<Func<SQLiteAsyncConnection, Task>> _migrations =
            new List<Func<SQLiteAsyncConnection, Task>>
                {
                    MultiplyAllAmountFields,
                    SeedDatabase//NB: This should ALWAYS be last
                };

        public static async Task MigrateDatabaseIfNesscary()
        {
            var connection = DatabaseManager.Instance.Connection;
            var currentVersion = await connection.Table<MetaData>().Where(x => x.Key == MetaData.VERSION_KEY).FirstOrDefaultAsync();

            int currentMigration = 0;
            if (currentVersion != null)
                currentMigration = currentVersion.ValueAsInt();

            if (currentMigration >= _migrations.Count)
                return;

            for (int i = currentMigration; i < _migrations.Count; i++)
                await _migrations[i](connection);

            if (currentVersion != null)
            {
                currentVersion.Value = _migrations.Count.ToString();
                await connection.UpdateAsync(currentVersion);
            }
            else
            {
                await connection.InsertAsync(new MetaData
                                                 {
                                                     Key = MetaData.VERSION_KEY,
                                                     Value = _migrations.Count.ToString()
                                                 });
            }
        }

        public static async Task SeedDatabase(SQLiteAsyncConnection connection)
        {
            var rows = await connection.QueryAsync<object>("SELECT * FROM sqlite_sequence");

            if (rows == null || rows.Count == 0)
            {
                await DefaultBudget.CreateDefaultBudget();
            }
        }

        private static async Task MultiplyAllAmountFields(SQLiteAsyncConnection connection)
        {
            await connection.ExecuteAsync(@"UPDATE ExpenseCategory SET 
                                            BudgetedPercentage = ROUND(100 * BudgetedPercentage),
                                            BudgetedAmount = ROUND(100 * BudgetedAmount),
                                            CurrentBalance = ROUND(100 * CurrentBalance)");

            await connection.ExecuteAsync(@"UPDATE Income SET TotalAmount = ROUND(100 * TotalAmount);");

            await connection.ExecuteAsync(@"UPDATE IncomeItem SET Amount = ROUND(100 * Amount);");

            await connection.ExecuteAsync(@"UPDATE TransactionItem SET Amount = ROUND(100 * Amount);");

            await connection.ExecuteAsync(@"UPDATE Transfer SET Amount = ROUND(100 * Amount);");

        }
    }
}