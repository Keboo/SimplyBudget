using Microsoft.Extensions.DependencyInjection;

namespace SimplyBudget;

public static class DI
{
    private static readonly List<IServiceProvider> _providers = new();

    public static IReadOnlyList<IServiceProvider> Providers => _providers.AsReadOnly();

    public static T? GetService<T>()
    {
        lock (_providers)
        {
            return _providers.Count == 0
                ? throw new InvalidOperationException("No providers registered")
                : _providers.Select(provider => provider.GetService<T>())
                .FirstOrDefault(service => service != null);
        }
    }

    public static object? GetService(Type serviceType)
    {
        lock (_providers)
        {
            return _providers.Count == 0
                ? throw new InvalidOperationException("No providers registered")
                : _providers.Select(provider => provider.GetService(serviceType))
                .FirstOrDefault(service => service != null);
        }
    }

    public static void Register(IServiceProvider provider)
    {
        lock (_providers)
        {
            _providers.Insert(0, provider);
        }
    }

    public static bool Unregister(IServiceProvider provider)
    {
        lock (_providers)
        {
            return _providers.Remove(provider);
        }
    }

    /*
    [SetupMethod]
    public static void Initialize(IApplicationBuilder application)
    {
        //Any needed run-time configuration here
        application.ConfigureServices(collection =>
        {
            collection.AddSingleton<IDispatcher, Dispatcher>();
            collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            collection.AddSingleton(ctx => new Func<BudgetContext>(() => new BudgetContext(Properties.Settings.GetDatabaseConnectionString())));
            collection.AddTransient<ISnackbarMessageQueue, SnackbarMessageQueue>();
        });
    }
    */
}

public interface IDispatcher
{
    Task InvokeAsync(Action callback);
}

public class Dispatcher : IDispatcher
{
    public async Task InvokeAsync(Action callback) => await System.Windows.Application.Current.Dispatcher.InvokeAsync(callback);
}
