

using System;
using System.IO;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public class DatabaseManager
    {
        private string _databasePath;

        private static readonly Lazy<DatabaseManager> _instance = new Lazy<DatabaseManager>(() => new DatabaseManager());

        
        public static DatabaseManager Instance
        {
            get { return _instance.Value; }
        }

        public string CurrentDatabasePath
        {
            get { return _databasePath; }
        }

        private DatabaseManager()
        { }

        public async Task InitDatabase(string storageFolder, string? dbFileName = null)
        {
            _databasePath = Path.Combine(storageFolder, dbFileName ?? "data.db");
            //_connection = new SQLiteAsyncConnection(_databasePath);
            
            //Create all of the require table if they don't exist
            //await _connection.CreateTablesAsync(typeof(ExpenseCategory), typeof(Transaction),
            //                                    typeof(TransactionItem), typeof(Income), typeof(IncomeItem),
            //                                    typeof(Account), typeof(Transfer), typeof(MetaData));
            
            await DatabaseMigrations.MigrateDatabaseIfNesscary();
        }

        public static async Task<T> GetAsync<T>(int id) where T : BaseItem, new()
        {
            return default!;
            //return await Instance.Connection.GetAsync<T>(id);
        }
    }
}