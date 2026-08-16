# Aspire Vue App template
This template creates a [Vue Web App](https://vuejs.org/) solution with .NET Aspire orchestration, Identity authentication, and unit tests.


## Template
Create a new app in your current directory by running.

```cli
> dotnet new keboo.aspire -f vue
```

### Parameters
[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-f`, `--frontend` | The frontend framework to use. Options: `react`, `vue` | `react` |

**Example with the Vue frontend (used by this repository):**
```cli
> dotnet new keboo.aspire -f vue
```

**Example with the React frontend:**
```cli
> dotnet new keboo.aspire -f react
```


## Updating .NET Version

This template uses a `global.json` file to specify the required .NET SDK version. To update the .NET SDK version:

1. Update the `global.json` file in the solution root
2. Update the `<TargetFramework>` in the `csproj` files.

## Key Features

### Progressive Web App (PWA) Support
Both the SimplyBudgetWeb.Web includes full PWA support with:
- Service worker for offline functionality
- Web app manifest for install-to-homescreen capability
- Caching strategies for improved performance
- App icons (192x192 and 512x512)

**Vue/Vite PWA:**
The Vue frontend uses `vite-plugin-pwa` with Workbox for advanced caching strategies. 

Features include:
- Automatic service worker registration and updates
- Static asset precaching with Workbox
- Runtime caching for images and Google Fonts
- App Shell pattern for SPA navigation
- Customizable manifest configuration in `vite.config.ts`

Note: Service workers only work in production builds and over HTTPS (or localhost).

### Build Customization
[Docs](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory?view=vs-2022&WT.mc_id=DT-MVP-5003472)

### Centralized Package Management
[Docs](https://learn.microsoft.com/nuget/consume-packages/Central-Package-Management?WT.mc_id=DT-MVP-5003472)

### NuGet package source mapping
[Docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping?WT.mc_id=DT-MVP-5003472)

### GitHub Actions / Azure DevOps Pipeline
Build, test, and code coverage reporting included. Use `--pipeline` parameter to choose between GitHub Actions (default) or Azure DevOps Pipelines.

### Solution File Format (slnx)
By default, this template uses the new `.slnx` (XML-based solution) format introduced in .NET 9. This modern format is more maintainable and easier to version control compared to the legacy `.sln` format.

[Blog: Introducing slnx support in the dotnet CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/?WT.mc_id=DT-MVP-5003472)  
[Docs: dotnet sln command](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln?WT.mc_id=DT-MVP-5003472)

If you need to use the legacy `.sln` format, use the `--sln true` parameter when creating the template.

## Infrastructure

Terraform (`Infra/`) is configured to use existing shared Azure resources rather than provisioning new
ones, while SimplyBudget's own (non-shared) resources live in their own dedicated resource group:

Shared, existing infrastructure (referenced via Terraform data sources, not managed here):
- Resource Group: `KebooDev`
- ACR: `keboodevacr.azurecr.io`
- Container App Environment: `keboodev-env`
- SQL Server: `keboodev-sql`, Database: `keboodevdb`

SimplyBudget-specific infrastructure (managed by this Terraform config):
- Resource Group: `SimplyBudget` (`westus2`, configurable via `app_resource_group_name`/`location` in
  `Infra/variables.tf`)
- Managed identity, backend Container App, Application Insights, and the Azure Static Web App all live
  in this resource group
- Frontend hosting is provisioned as an Azure Static Web App, and backend CORS allows that origin
- Frontend production builds use Terraform's `backend_url` output

Because `keboodevdb` is shared across multiple apps, SimplyBudget's tables live in their own SQL
schema (`database_schema_name` in `Infra/variables.tf`, default `SimplyBudget`) instead of `dbo`. During
`terraform apply`, the app's managed identity is provisioned as a database user, the schema is created
if it doesn't already exist, and the identity's default schema is set to it. EF Core mirrors this via
`modelBuilder.HasDefaultSchema("SimplyBudget")` in `SimplyBudgetWeb.Data/BudgetWebContext.cs`, so all
tables and migrations are created under that schema, keeping this app's data isolated from other apps
in the same database.

Apply infrastructure changes:

```bash
terraform -chdir=Infra plan
terraform -chdir=Infra apply -auto-approve
```

## Deployment
Deployment is handled with the [Azure Development CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/?WT.mc_id=DT-MVP-5003472).

This can be [installed](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd?tabs=winget-windows%2Cbrew-mac%2Cscript-linux&pivots=os-windows&WT.mc_id=DT-MVP-5003472) with `winget install microsoft.azd` 

If you don't already have it installed, you will also need to install bicep as this is what 

You will first need to login using `azd auth login` to authenticate with the Azure account that will be used for deployment.

On your first time, you will need to run `azd init` and scan the current directory. It will prompt you to provide a unique name for the app. This information will be stored in a `.azure` directory. It will also generate an `azure.yaml` file as well as a `next-steps.md` file outlining how to continue with publishing.

## Initial Project Setup

To configure Azure AD App Registrations, GitHub Actions OIDC, and Terraform backend infrastructure, run the interactive setup script:

```powershell
.\Setup.ps1
```

The script will:
1. Prompt for your project name (defaults to the repository name)
2. Detect your GitHub remote and Azure subscription
3. Create Azure AD App Registrations with federated credentials for CI/CD
4. Create the Terraform backend storage account
5. Generate `Infra/azure.auto.tfvars` for subscription-scoped Terraform variables
6. Configure GitHub repository secrets

**Prerequisites:** [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli), [GitHub CLI](https://cli.github.com/), and [Terraform](https://www.terraform.io/downloads).

After running the setup script, initialize Terraform:

```bash
cd Infra
terraform init

terraform plan
```

### Local Development Sign-In (Entra ID)

Running the app locally with `aspire run` requires an Entra ID (Azure AD) App
Registration for MSAL sign-in. The AppHost exposes two parameters,
`entra-client-id` and `entra-tenant-id`, that are shared by both the backend
(`AzureAd:ClientId` / `AzureAd:TenantId`) and the frontend (MSAL's
`clientId` / `authority`) so a value only needs to be configured once.

Set them via user secrets on the AppHost project:

```bash
cd SimplyBudgetWeb.AppHost
dotnet user-secrets set "Parameters:entra-client-id" "<app-registration-client-id>"
dotnet user-secrets set "Parameters:entra-tenant-id" "<tenant-id>"
```

If left unset, they default to an empty string and the app will still start,
but MSAL sends an empty `client_id` to Entra ID, resulting in an
`AADSTS900144` sign-in error when you try to sign in.


