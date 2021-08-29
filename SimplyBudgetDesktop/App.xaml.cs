
using AutoDI;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
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
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
//#if DEBUG
//            try
//            {
//                _ = global::Windows.ApplicationModel.Package.Current;
//            }
//            catch (InvalidOperationException)
//            {
//                //This is throw when run outside of an MSIX deployment
//                Settings.Default.StorageLocation = Path.GetFullPath(@".\Database");
//            }
//#endif
            //ShutdownOnConnectionStringChanged();
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

            //void ShutdownOnConnectionStringChanged([Dependency] IMessenger? messenger = null)
            //    => messenger!.Register(this);
        }

        private static void MakeDataBackup()
        {
            string backupsDirectory = Path.Combine(Settings.GetStorageDirectory(), "Backups");
            DirectoryInfo backups;
            try
            {
                backups = Directory.CreateDirectory(backupsDirectory);
            }
            catch (UnauthorizedAccessException)
            {
                backupsDirectory = Path.Combine(Path.GetTempPath(), "SimplyBudget", "Backups");
                backups = Directory.CreateDirectory(backupsDirectory);
            }
            const int maxBackups = 30;
            var fileName = $"{DateTime.Now:yyyyMMddhhmmss}.db";
            try
            {
                string sourcePath = Settings.GetDatabasePath();
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, Path.Combine(backups.FullName, fileName));
                }
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
    }
}
