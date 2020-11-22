
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using System;
using System.IO;
using System.Linq;
using System.Windows;

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
            BudgetContext.Instance.Database.Migrate();
            base.OnStartup(e);
        }

        private static void MakeDataBackup()
        {
            DirectoryInfo backups = Directory.CreateDirectory("Backups");
            const int maxBackups = 30;
            var fileName = $"{DateTime.Now:yyyyMMddhhmmss}-{BudgetContext.FileName}";
            try
            {
                File.Copy(BudgetContext.FileName, Path.Combine(backups.FullName, fileName));
            }
            catch (FileNotFoundException)
            { }

            foreach (var oldBackup in backups.EnumerateFiles($"*-{BudgetContext.FileName}")
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
