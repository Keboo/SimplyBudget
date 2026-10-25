# Project Context

- **Owner:** Kevin Bost
- **Project:** SimplyBudget — personal budgeting and expense tracking, including pending expenses and receipt uploads.
- **Stack:** C#, .NET 10, .NET Aspire, Vue/Vite, TypeScript, and .NET/UI test projects.
- **Created:** 2026-10-18T23:22:18.977-07:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### Test runner & shared config (verified 2026-10-25)
- `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }` → `dotnet test` is MTP mode: use `dotnet test --project <csproj>` (VSTest-style `--filter`/`--logger` args from old READMEs don't apply).
- Central package mgmt in `Directory.Packages.props`: TUnit 1.73.19, MSTest 4.3.3, Moq 4.20.72 + Moq.AutoMock 4.0.3, Microsoft.AspNetCore.Mvc.Testing, EF InMemory/Sqlite, Aspire.Hosting.Testing 13.6.4, Microsoft.Playwright 1.63.0, Deque.AxeCore.Playwright. xunit is declared but unused by any test project.
- `SimplyBudgetWeb.slnx` lists `SimplyBudgetSharedTests.csproj` only as a Solution Item *file*, not a Project → solution build/test skips it (its bin was never built locally).

### SimplyBudgetSharedTests (MSTest, legacy shared library)
- MSTest 4 + `EnableMSTestRunner`; `[TestClass]/[TestMethod]/[DataRow]`, classic `Assert.IsTrue` etc., `//Arrange //Act //Assert` comments. ~60 tests in 10 classes under `Data\` and `Import\`.
- Helpers in `AutoMockerExtensions.cs`: `mocker.WithDbScope()` → file-based SQLite in `%TEMP%\SimplyBudgetTests\<guid>` with `Database.Migrate()`, returns a local `IDbContextFactory<BudgetContext>` (`Create()`); `WithMessenger()`, `WithSynchronousTaskRunner()` (swaps `TaskEx.Default`). `MessageWatcher<T>` records CommunityToolkit messenger messages.
- Run: `dotnet test --project SimplyBudgetSharedTests\SimplyBudgetSharedTests.csproj` (must build itself; NOT run in CI).

### SimplyBudgetWeb.Core.Tests (TUnit, main backend suite)
- TUnit (`OutputType Exe`), assertions `await Assert.That(x).IsEqualTo(..)/IsEquivalentTo/IsNotNull`; global usings Moq + Moq.AutoMock. 188 tests discovered (`--list-tests`, 2026-10-25).
- Naming: `Method_ExpectedBehavior[_Condition]` (e.g. `FindMatches_RequiresExactAmountWithinFiveDaysAndAnUnlinkedReceipt`). Folders: `Controllers\`, `Services\`, `Integration\`.
- Unit pattern: `AutoMocker mocker = new(); mocker.WithDbContext<BudgetWebContext>();` → in-memory SQLite (`DataSource=:memory:`, `EnsureCreated`, NoTracking, registers `SYSDATETIMEOFFSET` SQL fn) resolving `BudgetWebContext`, options and `IDbContextFactory`; seed via `mocker.InDbScopeAsync(ctx => ...)` (`AutoMockerExtensions.Database.cs`). Controllers are new'd directly with `Mock.Of<>`/`NullLogger`.
- Integration: `Integration\ReceiptsApiFactory.cs` = `WebApplicationFactory<Program>` with in-memory SQLite, `TestAuthHandler` ("Test" scheme, `groups` claim = `test-group`, toggle `Authenticated`), fakes `FakeReceiptImageStore`, `FakeReceiptAnalyzer` (TCS to control timing), `RecordingReceiptMonthNotifier`; disables warm-up/migrations via settings. Only receipts use it (`ReceiptsApiTests.cs`, 18 tests).
- Noise: `Tests.cs` + `Data\DataClass.cs`, `DataSourceGenerator.cs`, `DependencyInjectionClassConstructor.cs` are TUnit template samples (5 meaningless tests).
- Run: `dotnet test --project SimplyBudgetWeb.Core.Tests\SimplyBudgetWeb.Core.Tests.csproj` (filter with TUnit `--treenode-filter "/*/*/<Class>/*"`).

### SimplyBudgetWeb.UITests (TUnit + Playwright + Aspire)
- `UITestBase.cs`: `[Before(TestSession)]` starts AppHost via `DistributedApplicationTestingBuilder.CreateAsync<Projects.SimplyBudgetWeb_AppHost>` (container suffix "UITests", strips SQL Server volume/lifetime annotations → ephemeral SQL container; needs Docker), 3 start retries, frontend warm-up (Vite dev server cold start). Set `FRONTEND_URL` to target an already-running app instead. Chromium only; screenshots → `TestResults\screenshots`, logs → `TestResults\logs`; env `HEADLESS`, `SLOW_MO`, `SCREENSHOT_DIR`, `LOGS_DIR` (`PlaywrightConfiguration.cs`). axe a11y via `AssertNoAccessibilityViolations()` (WCAG 2.x A/AA), `[Category(TestCategories.Accessibility)]`.
- Real tests are anonymous-only smoke: `AuthTests`, `HomePageTests`, `AppNavigationTests` (redirect to /login). `AuthedUserTestBase`, `PageObjects\{Register,Login,MyRooms,ManageRoom,RoomView,Home,Account}Page` and `README.md` ("Q&A System", MSTest) are template leftovers; auth is Entra ID so register-flow login cannot work.
- Run: build, `pwsh SimplyBudgetWeb.UITests\bin\Debug\net10.0\playwright.ps1 install`, then `dotnet test --project SimplyBudgetWeb.UITests\SimplyBudgetWeb.UITests.csproj` (slow; Docker required).

### Frontend (SimplyBudgetWeb.Web, Vitest)
- `vitest.config.ts`: jsdom, `src/**/*.test.ts`, `vitest.setup.ts`, vuetify inlined, `define`s fake API/Entra globals. Uses `@vue/test-utils` + pinia; mocks `@/services/apiClient` via `vi.hoisted`/`vi.mock`.
- Tests: `src\pages\Receipts.test.ts` (large), `src\services\apiClient.test.ts`, `src\utils\currency.test.ts`, `src\utils\receiptImage.test.ts`.
- Run (in `SimplyBudgetWeb.Web`): `pnpm run test` (`vitest run`); lint `pnpm run lint`.

### CI (`.github\workflows\build-and-deploy.yml`, PRs/pushes to main)
- `build` job: pnpm lint → pnpm test → build; `dotnet build SimplyBudgetWeb.slnx -c Release`; `dotnet ef migrations has-pending-model-changes`; `dotnet test --project SimplyBudgetWeb.Core.Tests/... --no-build -c Release`.
- `ui-tests` job: runs inside `mcr.microsoft.com/playwright/dotnet:v1.63.0-noble` (keep tag in sync with Microsoft.Playwright), installs Docker CLI + Node/pnpm, uploads failure screenshots/logs. Both checks are required by the branch ruleset.
- SimplyBudgetSharedTests is not run anywhere in CI.

### Coverage gaps (2026-10-25)
- Thin controller coverage: Accounts (1 test), Receipts unit (2), Transactions/Budget/DataPortability (3 each), HistoryRealtime (1). Heavy: PendingExpenses (34), ExpenseCategories (22), Import (12), RuleMatcher (12).
- Untested services: `SimplyBudgetWeb\Services\ReceiptServices.cs` (real Azure blob/DocIntel impls), `BudgetMonthUpdateNotifier.cs` (SignalR), `ThemeService.cs`; `SimplyBudgetWeb.Core\{DatabaseMigrationService,StartupWarmupService,DependencyInjection}.cs`.
- HTTP-pipeline (auth/routing/binding) integration tests exist only for receipts; no authz tests for other endpoints.
- Frontend: no tests for Budget, History, PendingExpenses, Accounts, Import, Settings, Login pages, components other than receipt ones, or pinia stores.
- E2E: zero authenticated user journeys (expense, pending expense, receipt flows untested end-to-end).