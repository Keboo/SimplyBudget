# SimplyBudget — Agent Guide

## Repository Overview
SimplyBudget is a personal budget tracking application with both a WPF desktop client and a web-based interface.

### Projects
| Project | Purpose |
|---|---|
| `SimplyBudgetShared` | Shared entity models and SQLite data layer (desktop) |
| `SimplyBudgetDesktop` | WPF desktop application |
| `SimplyBudgetDesktop.Tests` | Desktop app tests |
| `SimplyBudgetSharedTests` | Shared library tests |
| `SimplyBudget.ServiceDefaults` | Aspire service defaults (OTEL, health checks) |
| `SimplyBudget.Data` | EF Core data layer for SQL Server (web) |
| `SimplyBudget.Api` | ASP.NET Core minimal API backend |
| `SimplyBudget.Api.Tests` | API tests |
| `SimplyBudget.AppHost` | Aspire AppHost (local dev orchestration) |
| `SimplyBudget.Web` | React/TypeScript/Vite frontend |

## Domain Model
All monetary amounts are stored as **integers (cents)**. Negative = expense, positive = income/credit.

- **Account** — a bank/credit account, has a current balance and validated date
- **ExpenseCategory** — a spending bucket with optional budget amount or percentage; can be grouped by `CategoryName`
- **ExpenseCategoryItem** — a transaction (has a date, description, 1+ details)
- **ExpenseCategoryItemDetail** — a line item linking a transaction to a category with an amount
- **ExpenseCategoryRule** — a regex rule that auto-assigns categories to transactions by description
- **Metadata** — key/value store for settings

## Data Layer Notes
- `SimplyBudget.Data` uses SQL Server with the `sb` schema (`HasDefaultSchema("sb")`)
- On `ExpenseCategoryItemDetail` Added: `category.CurrentBalance += detail.Amount`
- On `ExpenseCategoryItemDetail` Deleted: `category.CurrentBalance -= detail.Amount`
- On `ExpenseCategoryItem` Deleted: cascade-delete child details (and trigger above balance hooks)
- `SaveChanges` (sync) is blocked — always use `SaveChangesAsync`

## API Endpoints
All under `/api/`:
- `GET/POST /accounts`, `PUT/DELETE /accounts/{id}`
- `GET /budget?year&month`
- `GET/POST /expense-categories`, `PUT/DELETE /expense-categories/{id}`, `PATCH /{id}/hide`, `PATCH /{id}/show`
- `GET /transactions?year&month&search&categoryIds&accountId`, `POST /transactions`, `DELETE /transactions/{id}`
- `POST /transactions/income` — add income
- `POST /transactions/transfer` — transfer between categories
- `GET/POST /category-rules`, `PUT/DELETE /category-rules/{id}`, `GET /category-rules/match?description=`
- `POST /import` — parse CSV, `POST /import/confirm` — confirm parsed rows

## Local Development
1. Install .NET 10 SDK and Node 22+
2. Run the Aspire AppHost: `dotnet run --project SimplyBudget.AppHost`
3. Aspire starts a SQL Server container, the API, and the React dev server
4. Dashboard available at the Aspire dashboard URL printed on startup

## Infrastructure
- Backend: Azure Container App (`simplybudget-api`) in existing `keboodev-env`
- Frontend: Azure Static Web App (`simplybudget-swa`)
- Database: `SimplyBudget` database on existing `keboodevdb` SQL Server, `sb` schema
- ACR: `keboodevacr` (existing)
- New resources in `SimplyBudget` resource group

## CI/CD
- `.github/workflows/dotnet.yml` — builds and tests the desktop app
- `.github/workflows/web.yml` — builds, tests, and deploys the web app

## Required GitHub Secrets
| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | Service principal / OIDC client ID |
| `AZURE_TENANT_ID` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `SWA_DEPLOYMENT_TOKEN` | Static Web App deployment token |
