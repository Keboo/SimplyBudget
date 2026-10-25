# Project Context

- **Owner:** Kevin Bost
- **Project:** SimplyBudget — personal budgeting and expense tracking, including pending expenses and receipt uploads.
- **Stack:** C#, .NET 10, .NET Aspire, Vue/Vite, TypeScript, and .NET/UI test projects.
- **Created:** 2026-10-18T23:22:18.977-07:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### Solution & project graph (verified 2026-10-25)
- `SimplyBudgetWeb.slnx` lists 8 projects; `SimplyBudgetSharedTests` (MSTest) is only a *Solution Items file*, not a project -> not built/tested by `dotnet build/test` on the slnx nor by CI.
- Dependency chain: `SimplyBudgetShared` (EF Sqlite, CsvHelper, CommunityToolkit.Mvvm; legacy desktop-era domain: `BudgetContext`, entities, import, data portability) <- `SimplyBudgetWeb.Data` (`BudgetWebContext : BudgetContext`, adds PendingExpenses/Receipts; SqlServer) <- `SimplyBudgetWeb.Core` (thin: `AddDatabase`, `AddStartupWarmup`, `DatabaseMigrationService`, `ConnectionStrings.DatabaseKey="Database"`; csproj excludes `Services\**`) <- `SimplyBudgetWeb` (ASP.NET Core API: Controllers/, Hubs/BudgetMonthHub, Middleware/, Services/ incl. receipt Azure services) which also refs ServiceDefaults + `SimplyBudgetWeb.Web` (Vue SPA wrapper csproj).
- AppHost refs Core/Data with `IsAspireProjectResource="false"` (for EF design-time) and SimplyBudgetWeb as the project resource. UITests ref AppHost (Aspire.Hosting.Testing + Playwright + TUnit). Core.Tests (TUnit, exe) ref Core + SimplyBudgetWeb (`public partial class Program` for WebApplicationFactory).
- Domain/business logic mostly lives in `SimplyBudgetWeb\Services` and controllers, not in Core.

### Build configuration
- `global.json`: SDK 10.0.100 rollForward latestFeature; `test.runner = Microsoft.Testing.Platform` (use `dotnet test --project <csproj>`).
- `Directory.Build.props`: ImplicitUsings, Nullable enable, LangVersion 14 (C# 14 `extension(...)` blocks used in AppHost), shared `UserSecretsId=SimplyBudgetWeb-secrets` for ALL projects. `Directory.Build.targets` is empty.
- `Directory.Packages.props`: CPM + `CentralPackageTransitivePinningEnabled`. Mixed Aspire versions (13.6.4 vs Sql/SqlServer 13.5.4; AppHost SDK `Aspire.AppHost.Sdk/13.3.1`). Microsoft.Data.SqlClient pinned 7.1.1 to match `.Extensions.Azure` (else AmbiguousMatchException). Playwright 1.63.0 must match CI image tag `mcr.microsoft.com/playwright/dotnet:v1.63.0-noble`.
- `NuGet.config`: nuget.org only with source mapping `*`. `.config/dotnet-tools.json`: dotnet-ef 10.0.13, aspire.cli 13.6.4.
- `.editorconfig`: 4-space C#, CRLF, block-scoped namespaces (silent), explicit types preferred over `var` (silent), usings outside namespace.
- Backend csproj tuned for cold start: ReadyToRun only when RID set, workstation GC, `ContainerFamily=noble-chiseled-extra` (ICU kept; no AOT/trimming by design).
- Frontend: pnpm (scripts dev/build=`vue-tsc -b && vite build`/lint/test=`vitest run`); Web.csproj runs `pnpm install --frozen-lockfile` on Debug build if node_modules missing.

### Aspire AppHost (`SimplyBudgetWeb.AppHost\AppHost.cs`)
- `aspire.config.json` points at AppHost csproj; run with `aspire run`. Resources: existing ACA env `keboodev-env` (L9-12), params `entra-client-id`/`entra-tenant-id` default "" (L22-25, set via AppHost user-secrets `Parameters:entra-client-id`).
- Run mode: SQL Server container `SimplyBudgetWeb-sql` (persistent, image 2025-latest, volume `SimplyBudgetWeb-database`) + db `SimplyBudgetWeb-db`; DBGate explicit-start container; Azurite blobs `receipt-blobs` -> `Receipts__StorageConnectionString`. Publish mode: `AddConnectionString("Database")`, `RunMigrationsOnStartup=false`.
- Backend resource `SimplyBudgetWeb-backend` gets `ConnectionStrings__Database` + `AzureAd__*`; frontend `SimplyBudgetWeb-frontend` = `AddJavaScriptApp(..., "dev").WithPnpm()`, gets `REACTAPP_BACKEND_HTTP(S)`, `ENTRA_CLIENT_ID/TENANT_ID`. Vite proxies `/api` and `/hubs` (ws) to backend (`vite.config.ts`).
- Local migrations: db `OnResourceReady` shells `dotnet ef database update --no-build` using `DOTNET_BUILD_CONFIGURATION` (default Debug) -> must match built config (`Resources.cs` ~L290-470). Dashboard commands: Create/Remove/Apply Migration, Restore Tools, Run UI Tests (backend). EF design-time factory: `AppHost\DevelopmentAppDbContextFactory.cs` (startup project for `dotnet ef`).

### Backend cross-cutting (`SimplyBudgetWeb\Program.cs`)
- `AddServiceDefaults()` (OTel; OTLP if `OTEL_EXPORTER_OTLP_ENDPOINT`; Azure Monitor if `APPLICATIONINSIGHTS_CONNECTION_STRING`; `/health`, `/alive` anonymous) -> `AddDatabase()` (`UseAzureSql`, DbContextFactory + pool, history table in schema `SimplyBudget`) -> `AddStartupWarmup()` (`WarmUpOnStartup`, default true).
- Auth: `AddMicrosoftIdentityWebApiAuthentication(config,"AzureAd")`; SignalR token via `access_token` query only on `BudgetMonthHub.HubPath`; Default AND Fallback policy = `SimplyBudgetUsers` requiring `groups` claim == `Authorization:SimplyBudgetUsersGroupId` (throws if unset) (L84-99). `CurrentUserSyncMiddleware` records signed-in users as assignees.
- CORS `AllowFrontend`: dev = any localhost origin; prod = `AllowedOrigins` array. Swagger dev-only. Prod serves SPA static files + `MapFallbackToFile("index.html")`, though prod frontend is actually on Static Web Apps.
- Config keys: `ConnectionStrings:Database`, `AzureAd:*`, `Authorization:SimplyBudgetUsersGroupId`, `AllowedOrigins`, `Receipts:{StorageConnectionString|StorageAccountUri|StorageContainer|DocumentIntelligenceEndpoint}`, `AZURE_CLIENT_ID` (managed identity), `RunMigrationsOnStartup`, `WarmUpOnStartup`.

### CI/CD (`.github\workflows\build-and-deploy.yml`)
- build: pnpm lint/test/build -> `dotnet build SimplyBudgetWeb.slnx -c Release` -> `dotnet ef migrations has-pending-model-changes --project SimplyBudgetWeb.Data --startup-project SimplyBudgetWeb.AppHost` -> `dotnet test --project SimplyBudgetWeb.Core.Tests/SimplyBudgetWeb.Core.Tests.csproj` -> efbundle (main only).
- ui-tests job: Playwright docker container, host network, docker socket, `DOTNET_BUILD_CONFIGURATION=Release`. Dependabot PRs auto-merge after build+ui-tests.
- main deploy: terraform outputs -> deploy-database (efbundle) -> deploy-backend (`dotnet publish -r linux-x64 -t:PublishContainer` to ACR, update ACA) ; deploy-frontend (Static Web App, `BACKEND_URL` from TF). Infra changes go via `deploy-infrastructure.yml` (plan on PR, apply on main).

### Infra (Terraform, `Infra\`)
- Root `main.tf` uses only `./prod` module (`Infra\shared\` not referenced by root). State in azurerm backend `simplybudget-terraform-rg/simplybudgetinfra`, OIDC. Shared existing: RG `KebooDev`, ACR, ACA env `keboodev-env`, SQL `keboodev-sql`/`keboodevdb` (schema `SimplyBudget`). Owned RG `SimplyBudget`: UAMI, backend ACA (min 0 replicas, 60-min cooldown, port 8080), SWA (Free), App Insights, receipts storage (no shared keys) + Document Intelligence.
- Backend env vars are set in `Infra\prod\main.tf` `module "backend_container_app"` (~L330-372); SPA redirect URIs managed by `azuread_application_redirect_uris.webapp_spa` (imported in root `main.tf`). Keep redirect URIs and `AllowedOrigins__*` in sync.

### Repo conventions / gotchas
- `AGENTS.md` = Aspire agent guidance; `.github\copilot-instructions.md` = Squad coding-agent instructions (different files). README's "Deployment via azd" section is template leftover; real deploy is GH Actions + Terraform. `Setup.ps1` bootstraps Entra app regs/OIDC/TF backend.
- Package catalog still carries WPF/desktop packages (MaterialDesignThemes, Velopack) though no desktop project is in the repo.
