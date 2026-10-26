using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Integration;

/// <summary>
/// Hosts the real ASP.NET Core pipeline (routing, multipart binding, authorization, middleware)
/// with SQLite, a signed-in test user, and fake Azure receipt integrations.
/// </summary>
public sealed class ReceiptsApiFactory : WebApplicationFactory<Program>
{
    public const string GroupId = "test-group";

    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public FakeReceiptImageStore ImageStore { get; } = new();
    public FakeReceiptAnalyzer Analyzer { get; } = new();
    public FakeReceiptCornerDetector CornerDetector { get; } = new();
    public RecordingReceiptMonthNotifier MonthNotifier { get; } = new();
    public bool Authenticated { get; set; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Development");
        foreach (var (key, value) in new Dictionary<string, string>
        {
            ["ConnectionStrings:Database"] = "Server=unused",
            ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
            ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000001",
            ["Authorization:SimplyBudgetUsersGroupId"] = GroupId,
            ["WarmUpOnStartup"] = "false",
            ["RunMigrationsOnStartup"] = "false",
        })
        {
            builder.UseSetting(key, value);
        }
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BudgetWebContext>>();
            services.RemoveAll<IDbContextFactory<BudgetWebContext>>();
            services.RemoveAll<BudgetWebContext>();
            // Also drops the internal pool/lease registrations from AddDbContextPool.
            foreach (var descriptor in services
                .Where(x => x.ServiceType.IsGenericType && x.ServiceType.GenericTypeArguments.Contains(typeof(BudgetWebContext)))
                .ToList())
            {
                services.Remove(descriptor);
            }
            services.AddDbContext<BudgetWebContext>(options => options.UseSqlite(connection));

            services.RemoveAll<IReceiptImageStore>();
            services.RemoveAll<IReceiptAnalyzer>();
            services.AddSingleton<IReceiptImageStore>(ImageStore);
            services.AddSingleton<IReceiptAnalyzer>(Analyzer);
            services.RemoveAll<IReceiptCornerDetector>();
            services.AddSingleton<IReceiptCornerDetector>(CornerDetector);
            services.RemoveAll<IBudgetMonthUpdateNotifier>();
            services.AddSingleton<IBudgetMonthUpdateNotifier>(MonthNotifier);

            services.AddSingleton(this);
            services.AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.Scheme;
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            });
        });
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }

    public async Task EnsureDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BudgetWebContext>().Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            connection.Dispose();
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ReceiptsApiFactory factory) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public new const string Scheme = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!factory.Authenticated)
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
        [
            new Claim("groups", ReceiptsApiFactory.GroupId),
            new Claim("oid", "00000000-0000-0000-0000-0000000000aa"),
            new Claim("name", "Test User"),
            new Claim("preferred_username", "test@example.com"),
        ], Scheme);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
    }
}

public sealed class FakeReceiptImageStore : IReceiptImageStore
{
    private readonly Dictionary<string, byte[]> blobs = [];

    public Exception? UploadException { get; set; }
    public Exception? DeleteException { get; set; }
    public Func<Task>? AfterDelete { get; set; }

    public IReadOnlyDictionary<string, byte[]> Blobs => blobs;

    public async Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        if (UploadException is not null)
            throw UploadException;

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var blobName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        blobs[blobName] = buffer.ToArray();
        return blobName;
    }

    public Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken)
        => Task.FromResult<Stream>(new MemoryStream(blobs[blobName]));

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (DeleteException is not null)
            throw DeleteException;
        blobs.Remove(blobName);
        if (AfterDelete is not null)
            await AfterDelete();
    }
}

public sealed class RecordingReceiptMonthNotifier : IBudgetMonthUpdateNotifier
{
    public List<DateTime> Months { get; } = [];

    public Task NotifyMonthUpdated(DateTime date, CancellationToken cancellationToken = default)
        => NotifyMonthsUpdated([date], cancellationToken);

    public Task NotifyMonthsUpdated(IEnumerable<DateTime> dates, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Months.AddRange(dates.Select(x => new DateTime(x.Year, x.Month, 1)).Distinct());
        return Task.CompletedTask;
    }
}

public sealed class FakeReceiptCornerDetector : IReceiptCornerDetector
{
    public Exception? Exception { get; set; }
    public ReceiptImageGeometry? Result { get; set; }
    public List<(byte[] Image, string ContentType)> Calls { get; } = [];

    public Task<ReceiptImageGeometry?> DetectAsync(byte[] image, string contentType, CancellationToken cancellationToken)
    {
        Calls.Add((image, contentType));
        return Exception is null ? Task.FromResult(Result) : Task.FromException<ReceiptImageGeometry?>(Exception);
    }
}

public sealed class FakeReceiptAnalyzer : IReceiptAnalyzer
{
    public Exception? Exception { get; set; }
    public TaskCompletionSource<ReceiptExtraction>? Completion { get; set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ReceiptExtraction Result { get; set; } = new(
        "Corner Cafe",
        new DateTime(2026, 3, 4),
        1250,
        [new ExtractedReceiptLine("Latte", 450), new ExtractedReceiptLine("Sandwich", 800)]);

    public Task<ReceiptExtraction> AnalyzeAsync(byte[] image, CancellationToken cancellationToken)
    {
        Started.TrySetResult();
        return Completion?.Task ?? (Exception is null ? Task.FromResult(Result) : Task.FromException<ReceiptExtraction>(Exception));
    }
}
