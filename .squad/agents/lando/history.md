# Project Context

- **Owner:** Kevin Bost
- **Project:** SimplyBudget — personal budgeting and expense tracking, including pending expenses and receipt uploads.
- **Stack:** C#, .NET 10, .NET Aspire, Vue/Vite, TypeScript, and .NET/UI test projects.
- **Created:** 2026-10-18T23:22:18.977-07:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-10-25 — Data-layer skill-up (verified by reading source)

**ORM / provider**
- EF Core (net10.0). Web: `UseAzureSql` (SQL Server) in `SimplyBudgetWeb.Core/DependencyInjection.cs` `AddDatabase()`; registers BOTH `AddDbContextFactory` and `AddDbContextPool<BudgetWebContext>`. Conn string key `Database` (`SimplyBudgetWeb.Core/ConnectionStrings.cs`).
- Desktop/shared: `SimplyBudgetShared/Data/BudgetContext.cs` defaults to SQLite (`Data Source=data.db`). Its `OnConfiguring` always adds `LogTo(Debug)` + `EnableSensitiveDataLogging()` (inherited by the web context too).
- `BudgetWebContext : BudgetContext` (`SimplyBudgetWeb.Data/BudgetWebContext.cs`); default schema `SimplyBudget` (also used for `__EFMigrationsHistory`).

**SaveChanges hooks (important behavior)**
- `BudgetContext.SaveChanges()` (sync) THROWS — always use `SaveChangesAsync`.
- `SaveChangesAsync` pumps tracked entries and calls `IBeforeCreate`/`IBeforeRemove`: `ExpenseCategoryItemDetail` adjusts `ExpenseCategory.CurrentBalance` (+Amount on create, -Amount on remove); `ExpenseCategoryItem.BeforeRemove` loads and removes its details; `ExpenseCategory.BeforeCreate` assigns the default account. Then sends `DatabaseEvent<T>` through the MVVM messenger after the save. Bulk `ExecuteDelete/ExecuteUpdate` BYPASSES these hooks (used only in `SimplyBudgetShared/DataTransfer/BudgetDataPortabilityService.cs` import, inside a transaction).
- Helpers `AddTransaction/AddIncome/AddTransfer` in `BudgetContextExtensions.cs` call `SaveChangesAsync` internally (debits stored as negative detail amounts).

**Entities & relationships** (int keys; shared entities use `BaseItem.ID`, receipts use `Id`)
- Account 1─* ExpenseCategory (`AccountID` optional, no explicit OnDelete → ClientSetNull). Index: Account.IsDefault, ExpenseCategory.CategoryName.
- ExpenseCategoryItem 1─* ExpenseCategoryItemDetail (Cascade); ExpenseCategory 1─* Detail (Cascade, required). Transfer = 2 details summing to 0. `IgnoreBudget` on item is [NotMapped], derived from details.
- ExpenseCategoryRule *─1 ExpenseCategory (optional); ExternalLinkRule, Metadata (key/value; `Version`, `CalculatorTaxOptions`) standalone.
- PendingExpense (web-only): Assignee → SetNull, SuggestedCategory → SetNull; index on Date; `[Timestamp] Version` rowversion.
- PendingExpenseAssignee: unique index on `ObjectId` (Entra `oid`), upserted at sign-in by `CurrentUserSyncService`; `IsNameCustomized` protects custom names.
- Receipt 1─* ReceiptLineItem (Cascade); index (TotalAmountCents, TransactionDate). ReceiptExpenseLink: PK = ReceiptId, unique ExpenseCategoryItemId → 1:1 Receipt↔ExpenseCategoryItem, Cascade from BOTH sides (deleting an expense item silently unlinks its receipt).
- Mapping mixes attributes (`[Table]`, `[MaxLength]`, `[Timestamp]`) and fluent config in `OnModelCreating`. Money = int cents everywhere. Date setters truncate to `.Date`.

**Migrations**
- Web: `SimplyBudgetWeb.Data/Migrations` (SQL Server, `yyyyMMddHHmmss_PascalName`, latest `20261020053159_AddReceiptImageGeometry`). Shared/desktop: `SimplyBudgetShared/Migrations` (SQLite) — shared-entity changes are added to BOTH sets (e.g. `AddExpenseItemNotes` exists in each).
- Tooling: `dotnet-ef` 10.0.13 local tool (`.config/dotnet-tools.json`). AppHost commands in `SimplyBudgetWeb.AppHost/Resources.cs` (`AddSqlDatabase`): "Create Migration" = `dotnet ef migrations --startup-project ./SimplyBudgetWeb.AppHost --project ./SimplyBudgetWeb.Data --no-build add <Name>`; also "Remove Migration" and "Apply Database Migrations" (`dotnet ef database update --no-build --configuration <cfg> ...`), auto-applied on DB resource ready locally.
- Design-time: `DesignTimeAppDbContextFactory` (AppHost, via `AddDatabase()` + `ConnectionStrings__Database`) and `BudgetWebContextDesignTimeFactory` (Data, localhost trusted conn).
- CI (`.github/workflows/build-and-deploy.yml`): `dotnet ef migrations has-pending-model-changes --project SimplyBudgetWeb.Data/... --startup-project SimplyBudgetWeb.AppHost/...` gates builds; main builds `dotnet ef migrations bundle --self-contained ... --output efbundle`, run by `deploy-database` job before `deploy-backend`. Prod sets `RunMigrationsOnStartup=false`; `DatabaseMigrationService` (MigrateAsync) only runs if that flag is true.
- No `HasData` seeding. `SampleBudget.GenerateBudget` (Shared) is a code-based sample generator only.

**Provisioning / storage**
- AppHost (`SimplyBudgetWeb.AppHost/AppHost.cs`): run mode = Aspire SQL Server container + db `SimplyBudgetWeb-db` (+ optional DbGate); publish mode = existing Azure SQL via `ConnectionStrings__Database` from Terraform.
- Receipt images: Azure Blob (`AzureReceiptImageStore`, `SimplyBudgetWeb/Services/ReceiptServices.cs`); blob name = `{Guid:N}{ext}`, container `receipts`. Local = Azurite (`Receipts__StorageConnectionString`); prod = `Receipts:StorageAccountUri` + managed identity (`Infra/prod/receipts.tf`). DB stores only BlobName/FileName/ContentType/geometry JSON (≤1024, validated by `ReceiptImageGeometry`).

**Query patterns / concurrency**
- Contexts are injected directly into controllers (scoped, pooled). Reads use `Include` (no projections-to-DTO in SQL, no paging/Skip-Take, no split queries); mutations use explicit `.AsTracking()` defensively. `BudgetMonthDataCache` caches month reads — writers must call `InvalidateMonth(s)`.
- Optimistic concurrency only on PendingExpense: client sends base64 `Version` (body or `If-Match`), set as OriginalValue; `DbUpdateConcurrencyException` → 409.
- Receipts: Serializable transactions inside `CreateExecutionStrategy().ExecuteAsync` for upload-analysis save and delete; blob deleted BEFORE row delete.
- Risk noted: `PendingExpensesController.Convert` saves the expense via `AddTransaction` (one SaveChanges) then removes pending + adds receipt link in a second SaveChanges — not atomic; a version conflict leaves a created expense item behind.
- No soft deletes, no per-user ownership/tenancy columns: single shared budget gated by Entra group policy `SimplyBudgetUsers` (`SimplyBudgetWeb/Program.cs`).

**Tests**
- `SimplyBudgetWeb.Core.Tests/AutoMockerExtensions.Database.cs`: `mocker.WithDbContext<BudgetWebContext>()` → in-memory SQLite, `EnsureCreated` (not migrations), default NoTracking; `InDbScopeAsync`. SQLite gets `randomblob(8)` default for `Version`.
- `Integration/ReceiptsApiFactory.cs`: WebApplicationFactory swaps pooled context for SQLite + fake `IReceiptImageStore`/`IReceiptAnalyzer`. `SimplyBudgetSharedTests/AutoMockerExtensions.cs`: temp-file SQLite with `Database.Migrate()` (exercises shared migrations).