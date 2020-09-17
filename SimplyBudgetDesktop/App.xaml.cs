
using SimplyBudgetShared.Data;
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
            BudgetContext.Instance.Database.EnsureCreated();
            base.OnStartup(e);
        }
    }
}
