using CommunityToolkit.Mvvm.Messaging;

using Moq.AutoMock;

using SimplyBudget;

using SimplyBudgetSharedTests;

namespace SimplyBudgetDesktop.Tests;

public static class AutoMockerExtensions
{
    public static AutoMocker WithDefaults(this AutoMocker mocker)
        => mocker.WithMessenger()
                 .WithCurrentMonth();

    public static AutoMocker WithCurrentMonth(this AutoMocker mocker, DateTime? month = null)
    {
        var currentMonth = new CurrentMonth(mocker.Get<IMessenger>());
        if (month != null)
        {
            currentMonth.CurrenMonth = month.Value;
        }
        mocker.Use<ICurrentMonth>(currentMonth);
        return mocker;
    }

    public static IDisposable WithAutoDIResolver(this AutoMocker mocker)
    {
        var provider = new AsyncLocalServiceProvider(mocker);
        StaticDI.Register(provider);
        return new Disposable(() => StaticDI.Unregister(provider));
    }

    private class Disposable : IDisposable
    {
        public Disposable(Action onDispose)
        {
            OnDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        private Action OnDispose { get; }

        public void Dispose() => OnDispose();
    }

    private class AsyncLocalServiceProvider : IServiceProvider
    {
        private static readonly AsyncLocal<AutoMocker?> _AsyncLocalBuilder = new();

        private static AutoMocker? CurrentBuilder
        {
            get => _AsyncLocalBuilder.Value;
            set => _AsyncLocalBuilder.Value = value;
        }

        public AsyncLocalServiceProvider(AutoMocker mocker)
        {
            CurrentBuilder = mocker ?? throw new ArgumentNullException(nameof(mocker));
        }

        public object GetService(Type serviceType)
        {
            AutoMocker? mocker = CurrentBuilder;
            _ = mocker ?? throw new InvalidOperationException($"'mocker' was null when resolving {serviceType.FullName}.");

            return mocker.Get(serviceType);
        }
    }
}
