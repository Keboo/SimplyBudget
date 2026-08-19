using System.Collections.Concurrent;
using System.Text;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SimplyBudgetWeb.AppHost;

namespace SimplyBudgetWeb.UITests;

/// <summary>
/// Base class for UI tests that configures Playwright browser
/// </summary>
[Timeout(120_000)]
public abstract class UITestBase : IAsyncDisposable
{
    protected const int TestTimeoutMs = 120_000;

    // StartAspireHost below performs up to four sequential steps (build, start, wait-for-
    // frontend-healthy, warm-up-frontend), each individually allowed up to AspireDefaultTimeout
    // (or WarmupTimeoutMs for the last one). The hook-level timeout must therefore comfortably
    // exceed the sum of those steps, not match a single one of them, or a slow-but-otherwise-
    // healthy container pull (e.g. a cold Docker image cache on a CI runner) can trip the outer
    // timeout even though no individual step actually hung.
    private const int AspireHostStartupTimeoutMs = 600_000;
    private static TimeSpan AspireDefaultTimeout { get; set; } = TimeSpan.FromMinutes(2);

    // The frontend is a plain `pnpm dev` (Vite) process rather than a pre-built static
    // bundle. Aspire's "healthy" resource notification only confirms the dev server's HTTP
    // endpoint has started accepting connections - it does NOT wait for Vite to finish
    // transforming the SPA entry point on its first request, which can take well over a
    // minute on a cold CI cache (the "ui-tests" workflow job installs Node.js/pnpm and all
    // frontend dependencies from scratch on every run). Without warming the frontend up
    // before any real test runs, whichever test happens to navigate first can hit a still-
    // blank page and time out waiting on a Playwright selector - this was observed causing
    // AppNavigationTests.AnonymousUserIsRedirectedToLoginFromCoreRoutes and
    // HomePageTests.HeaderBrandIsVisible to intermittently fail with a blank-page screenshot
    // even though the app rendered fine moments later.
    private const int WarmupTimeoutMs = 120_000;
    private const int MaxWarmupAttempts = 3;
    private static DistributedApplication? _aspireAppHost = null;

    protected static AxeRunOptions AxeOptions => new()
    {
        RunOnly = new RunOnlyOptions
        {
            Type = "tag",
            // Focus on WCAG 2.x AA compliance (the commonly accepted standard)
            // AAA is excluded as it requires stricter color contrast ratios (7:1)
            // that are difficult to achieve with standard UI frameworks
            Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]
        },
        ResultTypes = [ResultType.Incomplete, ResultType.Violations]
    };

    private const string STATE_FILE = ".state.json";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private CancellationTokenSource? _logCts;
    private ConcurrentQueue<string> _logLines = new();

    protected IPage Page { get; private set; } = null!;
    protected IBrowser Browser => _browser!;

    protected static string CreateUniqueId() => Guid.NewGuid().ToString("N")[..12];
    protected string TestPassword { get; } = "Test@Pass123!";
    protected string TestEmail { get; } = $"testuser{CreateUniqueId()}@example.com";

    protected string? StateId { get; set; }

    protected static Uri FrontendBaseUri
    {
        get
        {
            if (field is not null)
                return field;
            
            if (_aspireAppHost is null)
                throw new InvalidOperationException("Neither external frontend URL nor Aspire host is available");
            
            return _aspireAppHost.GetEndpoint(Resources.Frontend);
        }

        private set;
    }

    protected static CancellationToken CancellationToken =>
        TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

    [Before(TestSession), Timeout(AspireHostStartupTimeoutMs)]
    public static async Task StartAspireHost(CancellationToken cancellationToken)
    {
        // Check if an external frontend URL is provided
        var externalUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrWhiteSpace(externalUrl))
        {
            FrontendBaseUri = new Uri(externalUrl);
            Console.WriteLine($"Using external frontend at: {externalUrl}");
            Console.WriteLine("Skipping Aspire host creation - using externally running instance");
            return;
        }

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SimplyBudgetWeb_AppHost>([], (x, i) =>
            {
                i.Configuration!.AddInMemoryCollection(
                [
                    new(Resources.ContainerSuffixKey, "UITests")
                ]);
            });

        // Force the database to run in an in-memory containers
        var sqlServer = appHost.Resources.OfType<SqlServerServerResource>()
            .First(x => x.Name == Resources.SqlServer);
        foreach (var annotation in sqlServer.Annotations
            .ToList())
        {
            if (annotation is ContainerMountAnnotation or ContainerLifetimeAnnotation)
                sqlServer.Annotations.Remove(annotation);
        }

        // Build the aspire host
        var app = _aspireAppHost = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(AspireDefaultTimeout, cancellationToken);

        // Start the aspire host
        await app.StartAsync(cancellationToken)
            .WaitAsync(AspireDefaultTimeout, cancellationToken);

        // Wait for the front end to start
        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            Resources.Frontend, cancellationToken)
            .WaitAsync(AspireDefaultTimeout, cancellationToken);

        // See the WarmupTimeoutMs comment above for why this is necessary: "healthy" only
        // means the dev server accepted a TCP connection, not that it finished compiling.
        await WarmUpFrontendAsync(cancellationToken);
    }

    /// <summary>
    /// Forces the Vite dev server to finish transforming the SPA entry point before any real
    /// test navigates, so tests don't race a cold first-request compile. Retries a few times
    /// since the dev server can also transiently refuse/reset connections while it is still
    /// booting, independent of the compile-time issue this exists to guard against.
    /// </summary>
    private static async Task WarmUpFrontendAsync(CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await CreateBrowserAsync(playwright);

        Exception? lastError = null;
        for (var attempt = 1; attempt <= MaxWarmupAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await browser.NewPageAsync();
            try
            {
                await page.GotoAsync(FrontendBaseUri.ToString(), new() { Timeout = WarmupTimeoutMs });

                // The login page is what every anonymous route redirects to, so waiting for
                // its "Sign in" button confirms the SPA bundle actually rendered rather than
                // just that the HTTP connection succeeded.
                await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })
                    .WaitForAsync(new() { Timeout = WarmupTimeoutMs });

                Console.WriteLine($"Frontend warm-up succeeded on attempt {attempt}.");
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                Console.WriteLine($"Frontend warm-up attempt {attempt}/{MaxWarmupAttempts} failed: {ex.Message}");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        // Fail fast here with a clear, dedicated diagnostic instead of letting every real test
        // that happens to run first fail with an identical, seemingly-unrelated selector
        // timeout against what is still a blank page.
        throw new InvalidOperationException(
            $"Frontend did not become ready to serve requests after {MaxWarmupAttempts} warm-up attempts.",
            lastError);
    }

    [After(TestSession)]
    public static async Task StopAspireHost(CancellationToken cancellationToken)
    {
        if (_aspireAppHost != null)
        {
            await _aspireAppHost.DisposeAsync();
            _aspireAppHost = null!;
        }
    }

    [Before(Test), Timeout(TestTimeoutMs)]
    public async Task TestSetup(CancellationToken cancellationToken)
    {
        await BeforeTestSetupAsync();

        StartCollectingLogs();

        _playwright = await Playwright.CreateAsync();
        _browser = await CreateBrowserAsync(_playwright);
        _context = await CreateBrowserContextAsync(_browser, StateId is not null ? $"{StateId}_{STATE_FILE}" : null);

        Page = await _context.NewPageAsync();
        Page.SetDefaultTimeout(PlaywrightConfiguration.DefaultTimeout);
        Page.SetDefaultNavigationTimeout(PlaywrightConfiguration.DefaultTimeout);
        await AfterTestSetupAsync();
    }

    protected virtual async Task BeforeTestSetupAsync() { }

    protected virtual async Task AfterTestSetupAsync() { }

    [After(Test)]
    public async Task TearDownAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        StopCollectingLogs();
        await CaptureScreenshotOnFailureAsync(testContext);
        await CaptureLogsOnFailureAsync(testContext);
        await DisposeAsync();
    }

    private async Task CaptureScreenshotOnFailureAsync(TestContext testContext)
    {
        try
        {
            if (testContext.Execution.Result?.State is not TestState.Failed || Page is null || Page.IsClosed)
                return;

            var screenshotDir = PlaywrightConfiguration.ScreenshotDirectory;
            Directory.CreateDirectory(screenshotDir);

            var testName = testContext.Metadata.TestName;
            var className = testContext.Metadata.TestDetails.Class.ClassType.FullName;
            var sanitized = string.Join("_", $"{className}.{testName}".Split(Path.GetInvalidFileNameChars()));
            var screenshotPath = Path.Combine(screenshotDir, $"{sanitized}_{CreateUniqueId()}.png");

            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });

            Console.WriteLine($"Screenshot saved to: {screenshotPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }

    private void StartCollectingLogs()
    {
        // Skip log collection if using external AppHost
        if (_aspireAppHost is null)
            return;

        _logLines = new ConcurrentQueue<string>();
        _logCts = new CancellationTokenSource();

        var loggerService = _aspireAppHost.Services.GetRequiredService<ResourceLoggerService>();
        var appModel = _aspireAppHost.Services.GetRequiredService<DistributedApplicationModel>();

        foreach (var resource in appModel.Resources)
        {
            var resourceName = resource.Name;
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var batch in loggerService.WatchAsync(resourceName)
                        .WithCancellation(_logCts.Token))
                    {
                        foreach (var line in batch)
                        {
                            var prefix = line.IsErrorMessage ? "ERR" : "OUT";
                            _logLines.Enqueue($"[{resourceName}] [{prefix}] {line.Content}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when test ends
                }
            });
        }
    }

    private void StopCollectingLogs()
    {
        try
        {
            _logCts?.Cancel();
            _logCts?.Dispose();
            _logCts = null;
        }
        catch { }
    }

    private Task CaptureLogsOnFailureAsync(TestContext testContext)
    {
        try
        {
            if (testContext.Execution.Result?.State is not TestState.Failed || _logLines.IsEmpty)
                return Task.CompletedTask;

            var logsDir = PlaywrightConfiguration.LogsDirectory;
            Directory.CreateDirectory(logsDir);

            var testName = testContext.Metadata.TestName;
            var className = testContext.Metadata.TestDetails.Class.ClassType.FullName;
            var sanitized = string.Join("_", $"{className}.{testName}".Split(Path.GetInvalidFileNameChars()));
            var logPath = Path.Combine(logsDir, $"{sanitized}_{CreateUniqueId()}.log");

            var sb = new StringBuilder();
            while (_logLines.TryDequeue(out var line))
            {
                sb.AppendLine(line);
            }

            File.WriteAllText(logPath, sb.ToString());
            Console.WriteLine($"Aspire logs saved to: {logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to capture Aspire logs: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    protected static async Task<IBrowser> CreateBrowserAsync(IPlaywright playwright)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = PlaywrightConfiguration.Headless,
            SlowMo = PlaywrightConfiguration.SlowMo
        };

        return await playwright.Chromium.LaunchAsync(launchOptions);
    }

    protected static async Task<IBrowserContext> CreateBrowserContextAsync(IBrowser browser, string? storageStatePath = null)
    {
        return await browser.NewContextAsync(new BrowserNewContextOptions
        {
            StorageStatePath = storageStatePath,
            IgnoreHTTPSErrors = true
        });
    }

    protected static async Task<string> SaveStateAsync(IBrowserContext context, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        prefix ??= CreateUniqueId();

        await context.StorageStateAsync(new BrowserContextStorageStateOptions
        {
            Path = $"{prefix}_{STATE_FILE}"
        });

        return prefix;
    }


    protected async Task<AxeResult> AssertNoAccessibilityViolations()
    {
        // Some UI elements (e.g. error snackbars triggered by a failed API call) enter with
        // a CSS opacity/transform transition. Scanning mid-transition can make axe-core see a
        // transient, blended color that fails a color-contrast check even though the final
        // rendered state is compliant. Wait for network activity and any in-flight
        // animations/transitions to settle first so we assert against the final state.
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForAnimationsToSettleAsync();

        AxeResult result = await Page.RunAxe(AxeOptions);

        await Assert.That(result.Violations).IsEmpty();
        return result;
    }

    private async Task WaitForAnimationsToSettleAsync()
    {
        // Poll until we've observed a short stable window with no running animations. This
        // avoids a race where we happen to check in between an animation being scheduled and
        // actually starting to run.
        const int RequiredStableChecks = 3;
        const int PollDelayMs = 100;

        var stableChecks = 0;
        while (stableChecks < RequiredStableChecks)
        {
            bool hasRunningAnimations = await Page.EvaluateAsync<bool>(
                "() => document.getAnimations().some(a => a.playState === 'running')");

            stableChecks = hasRunningAnimations ? 0 : stableChecks + 1;

            await Task.Delay(PollDelayMs, CancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Page != null)
            await Page.CloseAsync();

        if (_context != null)
            await _context.CloseAsync();

        if (_browser != null)
            await _browser.CloseAsync();

        _playwright?.Dispose();

        GC.SuppressFinalize(this);
    }
}
