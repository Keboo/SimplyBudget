using AutoDI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Properties
{
    public static class DI
    {
        [SetupMethod]
        public static void Initialize(IApplicationBuilder application)
        {
            //Any needed run-time configuration here
            application.ConfigureServices(collection =>
            {
                collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                collection.AddSingleton(x => new BudgetContext(Settings.Default.DatabaseConnectionString));
            });
        }
    }
}
