
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.Properties;
using SimplyBudgetShared.Data;
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
            MakeDataBackup();
            using (var context = new BudgetContext(Settings.Default.DatabaseConnectionString))
            {
                context.Database.Migrate();
            }
#if DEBUG
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            theme.SetPrimaryColor(Colors.Orange);
            helper.SetTheme(theme);
#endif
            base.OnStartup(e);
        }

        private static void MakeDataBackup()
        {
            DirectoryInfo backups = Directory.CreateDirectory("Backups");
            const int maxBackups = 30;
            string filePath = BudgetContext.GetFilePathFromConnectionString(Settings.Default.DatabaseConnectionString);
            var fileName = $"{DateTime.Now:yyyyMMddhhmmss}.db";
            try
            {
                File.Copy(filePath, Path.Combine(backups.FullName, fileName));
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
