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

### Category actions and transfers

Expense categories open in an edit dialog from Settings or the Budget page's right-side
category actions menu (⋮). Choose **Create transfer** in that menu to select the category
as the destination. A negative balance prefills the positive amount needed to cover it;
zero or positive balances leave the amount blank. Choose a different source category and
review the amount before selecting **Save**; opening the dialog never posts a transfer.

### Budget spending averages

Budget spending averages cover 3, 6, or 12 calendar months ending with the selected month
(including that month), matching the category spending graph. Months without spending count as
zero; transfers, credits, and items marked Ignore Budget are excluded. Averages retain fractional
cents until currency formatting. On the Budget page they are hidden by default; select the
averages icon button beside a category's amounts to show them inline.

### Historical category budgets

The category spending graph uses the fixed budget applicable to each calendar month,
drawn as steps rather than interpolated changes. Budget edits take effect at the start
of the current month in the application's local time zone; the last edit within a month
wins, including changes to zero. Earlier months keep their previous budgets, even when
they have no spending. Percentage-budget periods appear in month tooltips, not as dollar
lines. Current budget and allocation behavior elsewhere is unchanged.

Existing categories without history use their existing budget as an undated baseline;
the first actual budget edit preserves that baseline and starts monthly history. Renames
and unchanged saves do not create history. History is included in data exports/imports
(format 3); older exports remain supported. Apply the new database migration before
deploying the updated application (both SQL Server web and SQLite shared migrations are included).

### Progressive Web App (PWA) Support
Both the SimplyBudgetWeb.Web includes full PWA support with:
- Service worker for offline functionality
- Web app manifest for install-to-homescreen capability
- Caching strategies for improved performance
- App icons (192x192 and 512x512)

**Vue/Vite PWA:**
The Vue frontend uses `vite-plugin-pwa` with Workbox for advanced caching strategies. 

Features include:
- Automatic service worker registration and updates; newly detected versions reload open tabs without an update toast (unsaved form edits may be lost)
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
- Managed identity, backend Container App, Application Insights, Azure Static Web App, private receipt
  Blob Storage, and Azure AI Document Intelligence (Foundry tool) all live in this resource group
- Receipt images are stored in a private container. The backend managed identity receives
  `Storage Blob Data Contributor` and `Cognitive Services User`; receipt analysis uses the
  `prebuilt-receipt` Document Intelligence model.
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

For local receipt uploads, configure the backend with a storage account URI, container name, and
Document Intelligence endpoint. The local developer identity (for example, the identity used by
`az login`) needs the same two data-plane roles:

```powershell
$env:Receipts__StorageAccountUri = "https://<storage-account>.blob.core.windows.net/"
$env:Receipts__StorageContainer = "receipts"
$env:Receipts__DocumentIntelligenceEndpoint = "https://<document-intelligence>.cognitiveservices.azure.com/"
$env:Receipts__FoundryEndpoint = "https://<foundry-resource>.openai.azure.com/"
$env:Receipts__FoundryCornerDeployment = "gpt-6-luna"
aspire run
```

Receipt photos are cropped automatically: when a photo is selected, the browser sends an
EXIF-oriented, downscaled copy to `POST /api/receipts/detect-corners`, which asks a vision model
deployed in Microsoft Foundry (`Receipts:FoundryEndpoint` + `Receipts:FoundryCornerDeployment`,
Terraform default `gpt-6-luna` on DataZoneStandard in eastus2; override `receipt_foundry_location` /
`receipt_corner_model_*` if your subscription has quota elsewhere) for the approximate receipt corners and which way its header faces.
The model only returns coordinates; the browser then snaps them to the paper edges it finds in the
pixels (a bright receipt on a darker background, keeping the model's outline when the edges are
unclear) and crops and perspective-corrects the original pixels with those corners.
The result can be adjusted with **Adjust corners** or discarded with **Skip — use original**, and the
untouched original is always uploaded alongside the corners. If Foundry is not configured or no
receipt is found, the photo falls back to manual corner adjustment. The app identity (or your local
developer identity) needs the **Cognitive Services OpenAI User** role on the Foundry resource.

Receipt photos are perspective-corrected in the browser before upload. The detected merchant,
transaction date, total, and available line items can be edited on the Receipts page. Suggested
conversion matches have an exact total and a date within five days; these are suggestions, not
requirements for attaching a receipt.

The Receipts page opens to **Pending** (unattached) receipts; **Categorized** holds receipts already
attached to expenses. Upload JPEG, PNG, or WebP images up to 15 MB (PDF upload is not supported).
While upload and AI analysis run, optional merchant/date/total overrides can be entered; these take
priority over the returned AI suggestions. Review the details and select **Save corrections** to
persist them. Unsaved drafts survive switching receipts or refreshing the list, but not leaving
the page or reloading the browser. Extraction failures keep the image available for manual entry.

When processing a pending debit, the compact **Receipt (optional)** section is collapsed by default
when there are no matches and opens automatically when suggestions are found. Expand it to
select one suggested receipt, **Browse all pending receipts**
to choose one with incorrect or missing AI details, or select **No receipt**. Previewing or
attaching it does not change the editable expense description, date, notes, or category amounts.
After conversion, **View receipt** in Expenses opens the original document. Receipt previews show
loading/error/retry states and offer an **Open original receipt** link (including for existing PDFs).
Deleting any receipt requires confirmation and permanently deletes its original image, receipt,
and attachment links. For categorized receipts, all expense data (amounts, categories, descriptions)
is kept. Missing receipts return 404; receipts still processing return 409. A storage failure returns
503 and keeps the receipt and its links for retry. Deleting an expense instead returns its receipt
to Pending.

If an upload succeeds but automatic extraction fails with `401 PermissionDenied`, check the
backend managed identity's role on the Document Intelligence resource. `Cognitive Services Data
Reader` cannot perform `Microsoft.CognitiveServices/accounts/FormRecognizer/documentmodels:analyze/action`;
the resource-scoped `Cognitive Services User` assignment in `Infra/prod/receipts.tf` permits analysis.
Apply the Terraform change and allow time for Azure RBAC propagation before uploading again.
The original image remains saved for manual review when extraction fails.

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

The App Registration must also have a SPA redirect URI matching wherever
you're running the frontend from (e.g. `http://localhost:5173` for
`aspire run`). Production's SPA redirect URIs (localhost, the Static Web
App's default hostname, and the `frontend_custom_domain` custom domain)
are managed via Terraform (`Infra/prod/main.tf`'s
`azuread_application_redirect_uris.webapp_spa` resource, imported into
state via a root-module `import` block in `Infra/main.tf` since it
already existed in Entra ID) — a mismatched redirect URI here results in
an `AADSTS50011` sign-in error.

The backend's CORS `AllowedOrigins` must be kept in sync with the same set
of hostnames (Terraform sets `AllowedOrigins__0`/`AllowedOrigins__1` env
vars on the backend Container App in `Infra/prod/main.tf`) — an origin
missing from this list results in a CORS "missing allowed origin" error
in the browser instead.
