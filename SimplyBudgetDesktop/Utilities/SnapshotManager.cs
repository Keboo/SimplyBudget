using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimplyBudget.Events;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.Utilities
{
    public class SnapshotManager
    {
        private const string DIRECTORY = "snapshots";
        private const string EXTENSION = "snapshot";
        private const char REPLACEMENT_CHAR = '_';

        private static readonly Lazy<SnapshotManager> _instance = new Lazy<SnapshotManager>(() => new SnapshotManager());

        public static SnapshotManager Instance => _instance.Value;

        private SnapshotManager()
        { }

        public Task CreateSnapshotAsync()
        {
            return Task.Run(() => CreateSnapshot());
        }

        public IEnumerable<string> GetSnapshotFiles()
        {
            var dbPath = DatabaseManager.Instance.CurrentDatabasePath;

            if (dbPath != null)
            {
                var directory = Path.GetDirectoryName(dbPath) ?? "";
                directory = Path.Combine(directory, DIRECTORY);
                if (Directory.Exists(directory))
                    return Directory.EnumerateFiles(directory, "*." + EXTENSION);
            }
            return new string[0];
        }

        private void CreateSnapshot()
        {
            var dbPath = DatabaseManager.Instance.CurrentDatabasePath;

            if (dbPath != null && File.Exists(dbPath))
            {
                var directory = Path.GetDirectoryName(dbPath) ?? "";
                directory = Path.Combine(directory, DIRECTORY);
                if (Directory.Exists(directory) == false)
                    Directory.CreateDirectory(directory);

                var snapshotName = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                snapshotName = Path.GetInvalidFileNameChars().Union(new[] { ' ' }).Aggregate(snapshotName, (current, @char) => current.Replace(@char, REPLACEMENT_CHAR));
                snapshotName = Path.ChangeExtension(snapshotName, EXTENSION);

                var targetPath = Path.Combine(directory, snapshotName);
                File.Copy(dbPath, targetPath);
                NotificationCenter.PostEvent(new SnapshotCreatedEvent());
            }
        }

        public async Task LoadSnapshot(string filePath)
        {
            var dbPath = DatabaseManager.Instance.CurrentDatabasePath;
            if (File.Exists(filePath) && File.Exists(dbPath))
            {
                File.Copy(filePath, dbPath, overwrite: true);
                await DatabaseManager.Instance.InitDatabase(Path.GetDirectoryName(dbPath), Path.GetFileName(dbPath));
                NotificationCenter.PostEvent(new SnapshotLoadedEvent());
            }
        }
    }
}