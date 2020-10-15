using Microsoft.Toolkit.Mvvm.Messaging;
using Moq.AutoMock;
using Moq.AutoMock.Resolvers;
using SimplyBudgetShared.Data;
using SimplyBudgetSharedTests.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetDesktop.Tests
{
    public static class AuotMockerExtensions
    {
        public static IDisposable BeginDbScope(this AutoMocker mocker, BudgetDatabaseContext? context = null)
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
            => mocker.WithMessenger();

        public static AutoMocker WithMessenger(this AutoMocker mocker)
        {
            mocker.Use<IMessenger>(new WeakReferenceMessenger());
            return mocker;
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
    }
}
