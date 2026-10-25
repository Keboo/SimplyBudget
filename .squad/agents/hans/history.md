# Project Context

- **Owner:** Kevin Bost
- **Project:** SimplyBudget — personal budgeting and expense tracking, including pending expenses and receipt uploads.
- **Stack:** C#, .NET 10, .NET Aspire, Vue/Vite, TypeScript, and .NET/UI test projects.
- **Created:** 2026-10-18T23:22:18.977-07:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### Backend skill-up (2026-10-25)

**Host & DI** (`SimplyBudgetWeb\Program.cs`)
- MVC controllers (`AddControllers` + `[ApiController]`, `[Route("api/...")]`), not minimal APIs. SignalR hub `BudgetMonthHub` at `/hubs/budget-month-updates`. Swagger only in Development. `public partial class Program;` exposed for `WebApplicationFactory`.
- Chain: `AddServiceDefaults()` → `AddDatabase()` → `AddStartupWarmup()` (`SimplyBudgetWeb.Core\DependencyInjection.cs`). DB = `UseAzureSql` with conn string key `Database`; registers BOTH `AddDbContextFactory` and `AddDbContextPool<BudgetWebContext>`; migrations history in schema `SimplyBudget`.
- Registrations: `CurrentUserSyncService`/`IBudgetMonthUpdateNotifier` scoped; `IBudgetMonthDataCache`, `IReceiptImageStore`(Azure), `IReceiptAnalyzer`(Azure) singletons. Most services live in `SimplyBudgetWeb\Services` (Core csproj has `<Compile Remove="Services\**">`; Core only holds DI, migration + warm-up hosted services).
- Controllers take `IBudgetMonthUpdateNotifier?`/`IBudgetMonthDataCache?` as optional ctor params defaulting to `Null*.Instance` — lets unit tests construct controllers without them.

**Auth** — Entra ID via `AddMicrosoftIdentityWebApiAuthentication(config, "AzureAd")`. Policy `SimplyBudgetUsers` = `RequireClaim("groups", Authorization:SimplyBudgetUsersGroupId)` set as Default AND Fallback policy, so controllers carry no `[Authorize]`. SignalR reads `access_token` query string only on hub path. `/health` and `/alive` are `AllowAnonymous` (ServiceDefaults). No per-user data partitioning — all authorized users share one budget.
- `CurrentUserSyncMiddleware` (after authZ) upserts `PendingExpenseAssignee` keyed by Entra `oid` on every request; best-effort (logs + `ChangeTracker.Clear()` on failure). `IsNameCustomized` stops overwriting user-set names.

**Error handling / validation**
- `ExceptionHandlingMiddleware`: `UnauthorizedAccessException`→403, `InvalidOperationException`→400, body `{statusCode,message}`. Other exceptions propagate (DeveloperExceptionPage in dev, `/Error` handler otherwise).
- Validation is hand-written in controllers returning `BadRequest("message")`/`Conflict(...)`/`NotFound()` with plain string bodies; no FluentValidation/DataAnnotations. Receipts integration failures use `Problem(503)`.
- `BudgetContext.SaveChanges()` (sync) throws — always use `SaveChangesAsync`.

**Money & dates** — all amounts are `int` cents. `ExpenseCategoryItem.Date`/`PendingExpense.Date` setters truncate to `.Date`. `PendingExpense.Amount` always positive with `IsDebit` flag; expense detail amounts are signed (transaction = negative, income = positive).

**Core domain rules** (`SimplyBudgetShared\Data\BudgetContextExtensions.cs`, entities)
- `AddTransaction`/`AddIncome`/`AddTransfer` extension methods create `ExpenseCategoryItem` + details and save immediately. `IgnoreBudget` must be set after `Details` (it fans out to details).
- Category balance is maintained by hooks: `ExpenseCategoryItemDetail.BeforeCreate/BeforeRemove` adjust `ExpenseCategory.CurrentBalance`; `ExpenseCategoryItem.BeforeRemove` deletes details. Hooks run in overridden `BudgetContext.SaveChangesAsync`, which also sends `WeakReferenceMessenger` `DatabaseEvent`s (desktop legacy).
- Transfer = exactly 2 details summing to 0 (`IsTransfer`). New category with no account gets default account.
- Category validation (`ExpenseCategoriesController.Validate`): name required; amount ≥0; percentage 0–100; not both amount and percentage; cap ≥0. Delete only if `CanDeleteAsync` (no details, no rule refs) else 409 "Hide it instead"; hide/restore toggles `IsHidden`.
- Remaining budget: `CalculateRemainingBudgetAmount` = max(0, min(budgeted−allocated, cap−allocated)); 0 for percentage categories. `BudgetController` total budget scales fixed amounts by `100/(100−totalPct)`; estimated income = 3-month avg of all-positive items.
- Rules: `ExpenseCategoryRuleMatcher.Match` — case-insensitive regex + optional |amount| min/max range; last matching category wins; notes de-duped and joined. Used at import save and `POST api/pending-expenses/reapply-rules` (never overwrites existing notes).

**Pending expenses** (`PendingExpensesController`, web-only entity in `BudgetWebContext`)
- Optimistic concurrency via `[Timestamp] Version` (base64 in DTO). PUT/convert require `Version` in body; DELETE requires `If-Match` header; mismatch → 409 "changed by another user". SQLite tests use `randomblob(8)` default.
- Convert: requires ≥1 item; debit→`AddTransaction`, credit→`AddIncome`; optional `ReceiptId` only for debits, receipt must exist and be unlinked (409); creates `ReceiptExpenseLink`, removes pending, returns 201.
- `DELETE api/pending-expenses` (no id) deletes everything matching current filters (month/search/assigneeId); no filters = delete all.
- CSV import (`ImportController`): `parse` uses `StcuCsvImport` (skips transfers, unknown type throws InvalidOperationException→400), flags duplicates by same date + signed amount across pending + existing items; `save` creates pending expenses for checked rows only.

**Receipts** (`ReceiptsController`, `Services\ReceiptServices.cs`)
- Upload: multipart `image` (+ optional `imageGeometry` corners JSON); ≤15 MB; jpg/png/webp with matching content type AND magic-byte check; filename ≤255. Blob name = GUID+ext. Status lifecycle `processing` → `needs-review` (after analysis) → `reviewed` (after PUT). Analysis failure never fails upload — sets `ProcessingMessage`.
- Post-analysis save and delete run inside `CreateExecutionStrategy` + Serializable transaction; delete refuses while `processing` (409), deletes blob first (503 on storage failure, receipt kept), then DB row with `CancellationToken.None`.
- Matches: `GET api/receipts/matches?amount&date` = unlinked receipts with exact total within ±5 days.
- Integrations: Azure Blob (`Receipts:StorageContainer` + `Receipts:StorageConnectionString` [Azurite locally via AppHost] or `Receipts:StorageAccountUri` + `DefaultAzureCredential` w/ `AZURE_CLIENT_ID`); Document Intelligence `prebuilt-receipt` (`Receipts:DocumentIntelligenceEndpoint`). Missing config → `ReceiptIntegrationNotConfiguredException` → 503.

**Caching & realtime** — `BudgetMonthDataCache` (IMemoryCache, 5 min, keyed `screen:{scope}:month:{yyyy-MM}:variant:...`, per-month CTS invalidation; invalidating a month also clears "all-months"). Every mutation must call `monthDataCache.Invalidate*` and, where budget totals change, `NotifyMonth(s)Updated` (SignalR `MonthUpdated` to group `month:yyyy-MM`). Category edits use `InvalidateAllMonths`.

**Config / hosted services** — `RunMigrationsOnStartup` (default false; publish uses an EF bundle in CI), `WarmUpOnStartup` (default true; warms DB + OIDC metadata after ApplicationStarted, best-effort). `AllowedOrigins` for prod CORS; dev allows any localhost origin.

**Telemetry** (`SimplyBudgetWeb.ServiceDefaults\Extensions.cs`) — OpenTelemetry logs/metrics/traces; OTLP if `OTEL_EXPORTER_OTLP_ENDPOINT`, Azure Monitor if `APPLICATIONINSIGHTS_CONNECTION_STRING`; health paths excluded from traces; `LogStartupTimings` logs cold-start milestones. Logging uses structured templates (`{ReceiptId}`).

**Conventions** — net10.0, C# 14 (`field` keyword, `extension(...)` blocks), Nullable + ImplicitUsings on (`Directory.Build.props`), central package mgmt (`Directory.Packages.props`), primary-constructor controllers, DTOs as positional `record`s at bottom of controller files, `AsTracking()` explicitly before mutating queries.

**Tests / commands** — `SimplyBudgetWeb.Core.Tests` uses TUnit (Microsoft.Testing.Platform per `global.json`), Moq.AutoMock with SQLite in-memory `WithDbContext<T>()`, and `ReceiptsApiFactory` (`WebApplicationFactory<Program>`) for full-pipeline receipt tests. CI command (`.github\workflows\build-and-deploy.yml`): `dotnet test --project SimplyBudgetWeb.Core.Tests/SimplyBudgetWeb.Core.Tests.csproj`.
