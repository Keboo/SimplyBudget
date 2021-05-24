using AutoDI;
using Microsoft.Toolkit.Mvvm.Messaging;
using Moq.AutoMock;
using Moq.AutoMock.Resolvers;
using SimplyBudget;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Threading;
using SimplyBudgetSharedTests.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimplyBudgetDesktop.Tests
{
    public static class AutoMockerExtensions
    {
        public static IDisposable WithSynchonousTaskRunner(this AutoMocker mocker)
        {
            TaskEx.Default = new SynchonousTaskScheduler();
            return new Disposable(() => TaskEx.Default = null!);
        }

        public static IDisposable WithDbScope(this AutoMocker mocker, BudgetDatabaseContext? context = null)
        {
            var resolver = new DbScopedResolver(context ?? new BudgetDatabaseContext());
            var existing = mocker.Resolvers.ToList();
            mocker.Resolvers.Clear();
            mocker.Resolvers.Add(resolver);
            foreach(var existingResolver in existing)
            {
                mocker.Resolvers.Add(existingResolver);
            }
            return resolver;
        }

        public static AutoMocker WithDefaults(this AutoMocker mocker)
            => mocker.WithMessenger()
                     .WithCurrentMonth();

        public static AutoMocker WithMessenger(this AutoMocker mocker)
        {
            mocker.Use<IMessenger>(new WeakReferenceMessenger());
            return mocker;
        }

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
            var provider = new AutoDIServiceProvider(mocker);
            GlobalDI.Register(provider);
            return new Disposable(() => GlobalDI.Unregister(provider));
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

        private class AutoDIServiceProvider : IServiceProvider
        {
            private static readonly AsyncLocal<AutoMocker?> _AsyncLocalBuilder = new();

            private static AutoMocker? CurrentBuilder
            {
                get => _AsyncLocalBuilder.Value;
                set => _AsyncLocalBuilder.Value = value;
            }

            public AutoDIServiceProvider(AutoMocker mocker)
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

        private class DbScopedResolver : IMockResolver, IDisposable
        {
            private bool _disposedValue;

            public DbScopedResolver(BudgetDatabaseContext context)
            {
                context.PerformDatabaseOperation(context => 
                {
                    Context = context;
                    return CompletionSource.Task;
                });
            }

            private BudgetContext? Context { get; set; }

            private TaskCompletionSource<object> CompletionSource { get; } = new TaskCompletionSource<object>();

            public void Resolve(MockResolutionContext context)
            {
                if (context.RequestType == typeof(BudgetContext))
                {
                    context.Value = Context;
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        CompletionSource.TrySetResult(null!);
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private class SynchonousTaskScheduler : TaskScheduler
        {
            public override int MaximumConcurrencyLevel => 1;

            protected override void QueueTask(Task task) => TryExecuteTask(task);

            protected override bool TryExecuteTaskInline(
                Task task,
                bool taskWasPreviouslyQueued) => TryExecuteTask(task);

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Enumerable.Empty<Task>();
            }
        }
    }
}
