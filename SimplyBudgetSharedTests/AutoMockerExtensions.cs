using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq.AutoMock;
using Moq.AutoMock.Resolvers;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Threading;

namespace SimplyBudgetSharedTests;

public static class AutoMockerExtensions
{
    public static IDisposable WithSynchronousTaskRunner(this AutoMocker _)
    {
        TaskEx.Default = new SynchronousTaskScheduler();
        return new Disposable(() => TaskEx.Default = null!);
    }

    public static IDbContextFactory<BudgetContext> WithDbScope(this AutoMocker mocker,
        IMessenger? messenger = null)
    {
        var resolver = new DbScopedResolver(messenger ?? mocker.Get<IMessenger>() ?? WeakReferenceMessenger.Default);
        var existing = mocker.Resolvers.ToList();
        mocker.Resolvers.Clear();
        existing.Insert(0, resolver);
        existing.Add(resolver);
        foreach (var existingResolver in existing)
        {
            mocker.Resolvers.Add(existingResolver);
        }
        return resolver;
    }

    public static AutoMocker WithMessenger(this AutoMocker mocker)
    {
        mocker.Use<IMessenger>(new WeakReferenceMessenger());
        return mocker;
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

    private sealed class DbScopedResolver : IMockResolver, IDbContextFactory<BudgetContext>
    {
        private bool _disposedValue;

        public DbScopedResolver(IMessenger messenger)
        {
            FilePath = Path.Combine(
                Path.GetTempPath(),
                "SimplyBudgetTests",
                Guid.NewGuid().ToString("N")
                );
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            using var context = Create();
            context.Database.Migrate();
        }

        private string FilePath { get; }
        private IMessenger Messenger { get; }

        public void Resolve(MockResolutionContext context)
        {
            if (context.RequestType == typeof(BudgetContext))
                context.Value = Create();
            else if (context.RequestType == typeof(Func<BudgetContext>))
            {
                context.Value = new Func<BudgetContext>(Create);
            }
        }

        public BudgetContext Create()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                DataSource = FilePath,
                Pooling = false
            };
            var options = new DbContextOptionsBuilder<BudgetContext>().UseSqlite(connectionString.ToString()).Options;
            return new BudgetContext(Messenger, options);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    File.Delete(FilePath);
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

    private class SynchronousTaskScheduler : TaskScheduler
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

public interface IDbContextFactory<TContext> : IDisposable
{
    TContext Create();
}
