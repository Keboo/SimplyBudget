using AutoDI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace SimplyBudget.Properties
{
    public static class DI
    {
        [SetupMethod]
        public static void Initialize(AutoDI.IApplicationBuilder application)
        {
            //Any needed run-time configuration here
            application.ConfigureServices(collection =>
            {
                collection.AddSingleton<IMessenger, Messenger>();
            });
        }
    }
}
