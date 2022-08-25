using AutoDI;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SimplyBudgetShared.Data;

namespace SimplyBudget;

public static class DI
{
    [SetupMethod]
    public static void Initialize(IApplicationBuilder application)
    {
        //Any needed run-time configuration here
        application.ConfigureServices(collection =>
        {
            collection.AddSingleton<IDispatcher, Dispatcher>();
            collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            collection.AddSingleton(ctx => new Func<BudgetContext>(() => new BudgetContext(Properties.Settings.GetDatabaseConnectionString())));
        });
    }
}

public interface IDispatcher
{
    Task InvokeAsync(Action callback);
}

public class Dispatcher : IDispatcher
{
    public async Task InvokeAsync(Action callback) => await System.Windows.Application.Current.Dispatcher.InvokeAsync(callback);
}
