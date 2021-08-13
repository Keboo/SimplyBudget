
using AutoDI;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudget.Properties;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Threading;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SimplyBudget
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : IRecipient<StorageLocationChanged>
    {
        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
            Settings.Default.StorageLocation = Path.GetFullPath(@".\Database");
#endif
            ShutdownOnConnectionStringChanged();
            MakeDataBackup();
            using (var context = new BudgetContext(Settings.GetDatabaseConnectionString()))
            {
                context.Database.Migrate();

                //TODO: Async this
                if (!context.ExpenseCategories.Any())
                {
                    TaskEx.Run(async () =>
                    {
                        await SampleBudget.GenerateBudget(context);
                        await context.SaveChangesAsync();
                    }).Wait();
                }
            }
#if DEBUG
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            theme.SetPrimaryColor(Colors.Orange);
            helper.SetTheme(theme);
#endif
            base.OnStartup(e);

            void ShutdownOnConnectionStringChanged([Dependency] IMessenger? messenger = null)
                => messenger!.Register(this);
        }

        private static void MakeDataBackup()
        {
            string backupsDirectory = Path.Combine(Settings.GetStorageDirectory(), "Backups");
            DirectoryInfo backups = Directory.CreateDirectory(backupsDirectory);
            const int maxBackups = 30;
            var fileName = $"{DateTime.Now:yyyyMMddhhmmss}.db";
            try
            {
                File.Copy(Settings.GetDatabasePath(), Path.Combine(backups.FullName, fileName));
            }
            catch (FileNotFoundException)
            { }

            foreach (var oldBackup in backups.EnumerateFiles($"*.db")
                .OrderByDescending(x => x.Name)
                .Skip(maxBackups)
                .ToList())
            {
                try
                {
                    oldBackup.Delete();
                }
                catch
                {
                    //TODO: Notification
                }
            }
        }

        public void Receive(StorageLocationChanged message) => Shutdown();
    }
}
