using SimplyBudgetWeb.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimplyBudgetWeb.Core;

/// <summary>
/// Warms up the expensive work that would otherwise be paid by the first user request after a
/// cold start: building the EF Core model, opening the first SQL connection (which may also have
/// to wait for a serverless Azure SQL database to resume), and downloading the Entra ID OpenID
/// Connect discovery/JWKS documents needed to validate the first bearer token.
/// </summary>
/// <remarks>
/// The warm-up deliberately runs only after <see cref="IHostApplicationLifetime.ApplicationStarted"/>
/// so it can never delay the host from listening or hold the startup health probe red. Every step
/// is best-effort: failures are logged and swallowed, because a failed warm-up should degrade the
/// first request to its normal (slow) path rather than take the application down.
/// </remarks>
internal sealed class StartupWarmupService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<StartupWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await WaitForApplicationStartedAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Run the independent warm-up paths concurrently; neither depends on the other.
        await Task.WhenAll(
            WarmUpDatabaseAsync(stoppingToken),
            WarmUpOpenIdConnectMetadataAsync(stoppingToken));
    }

    private async Task WaitForApplicationStartedAsync(CancellationToken stoppingToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var startedRegistration = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
        using var stoppingRegistration = stoppingToken.Register(() => started.TrySetCanceled(stoppingToken));
        await started.Task;
    }

    private async Task WarmUpDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startedAt = TimeProvider.System.GetTimestamp();

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BudgetWebContext>();

            // Opening the connection forces the EF model to be built and primes the connection
            // pool, including any wait for a serverless Azure SQL database to resume.
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await dbContext.Database.CloseConnectionAsync();

            logger.LogInformation(
                "Database warm-up completed in {ElapsedMilliseconds}ms",
                (long)TimeProvider.System.GetElapsedTime(startedAt).TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database warm-up failed; the first request will pay this cost instead");
        }
    }

    private async Task WarmUpOpenIdConnectMetadataAsync(CancellationToken cancellationToken)
    {
        // Entra ID is not necessarily configured locally (the AppHost defaults the client and
        // tenant ids to empty strings), and warming an unconfigured authority would only produce
        // a misleading warning on every local run.
        if (string.IsNullOrWhiteSpace(configuration["AzureAd:TenantId"]) ||
            string.IsNullOrWhiteSpace(configuration["AzureAd:ClientId"]))
        {
            return;
        }

        try
        {
            var startedAt = TimeProvider.System.GetTimestamp();

            var jwtOptions = serviceProvider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            if (jwtOptions.ConfigurationManager is null)
            {
                return;
            }

            await jwtOptions.ConfigurationManager.GetConfigurationAsync(cancellationToken);

            logger.LogInformation(
                "OpenID Connect metadata warm-up completed in {ElapsedMilliseconds}ms",
                (long)TimeProvider.System.GetElapsedTime(startedAt).TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenID Connect metadata warm-up failed; the first authenticated request will pay this cost instead");
        }
    }
}
