<#
.SYNOPSIS
    Interactive setup script that configures Azure AD App Registration and
    GitHub Actions OIDC authentication for SimplyBudget.

.DESCRIPTION
    This script:
    1. Creates an Azure AD App Registration with OIDC federated credentials
    2. Creates a Service Principal and assigns required Azure roles
    3. Sets GitHub repository secrets for CI/CD
    4. Optionally fetches and sets the Static Web App deployment token

    The script is idempotent and can be safely run multiple times.

.PARAMETER NonInteractive
    If specified, skips interactive prompts and uses parameter values or defaults.

.EXAMPLE
    .\Setup.ps1

.EXAMPLE
    .\Setup.ps1 -NonInteractive -SubscriptionId "00000000-0000-0000-0000-000000000000"

.EXAMPLE
    # After running 'terraform apply', set the SWA deployment token:
    .\Setup.ps1 -SetSwaToken
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectName = "SimplyBudget",

    [Parameter(Mandatory = $false)]
    [string]$GitHubOwner = "Keboo",

    [Parameter(Mandatory = $false)]
    [string]$GitHubRepo = "SimplyBudget",

    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $false)]
    [string]$Environment = "production",

    [Parameter(Mandatory = $false)]
    [string]$Branch = "main",

    [Parameter(Mandatory = $false)]
    [string]$SwaName = "simplybudget-swa",

    [Parameter(Mandatory = $false)]
    [string]$SwaResourceGroup = "SimplyBudget",

    # When specified, only fetches and sets SWA_DEPLOYMENT_TOKEN (run after terraform apply).
    [Parameter(Mandatory = $false)]
    [switch]$SetSwaToken,

    [Parameter(Mandatory = $false)]
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"

# ============================================================
# Helper Functions
# ============================================================

function Prompt-WithDefault {
    param(
        [string]$Message,
        [string]$Default
    )

    if ($Default) {
        $input = Read-Host "$Message [$Default]"
        if ([string]::IsNullOrWhiteSpace($input)) { return $Default }
        return $input
    }
    else {
        do {
            $input = Read-Host "$Message"
        } while ([string]::IsNullOrWhiteSpace($input))
        return $input
    }
}

function Prompt-YesNo {
    param(
        [string]$Message,
        [bool]$Default = $true
    )

    $suffix = if ($Default) { "[Y/n]" } else { "[y/N]" }
    $input = Read-Host "$Message $suffix"
    if ([string]::IsNullOrWhiteSpace($input)) { return $Default }
    return $input -match '^[Yy]'
}

function Create-AppRegistrationWithRoles {
    param(
        [string]$Name,
        [string]$SubscriptionId,
        [string]$GitHubOwner,
        [string]$GitHubRepo,
        [string]$Environment,
        [string]$Branch,
        [string[]]$Roles
    )

    Write-Host "`n  Checking for existing App Registration '$Name'..." -ForegroundColor Yellow
    $existingApp = az ad app list --display-name $Name --query "[0]" 2>$null | ConvertFrom-Json

    if ($existingApp) {
        Write-Host "  Found existing App Registration '$Name' (Client ID: $($existingApp.appId))" -ForegroundColor Green
        $ClientId = $existingApp.appId
        $AppObjectId = $existingApp.id
    }
    else {
        Write-Host "  Creating new App Registration '$Name'..." -ForegroundColor Yellow
        $app = az ad app create --display-name $Name | ConvertFrom-Json
        $ClientId = $app.appId
        $AppObjectId = $app.id
        Write-Host "  Created App Registration (Client ID: $ClientId)" -ForegroundColor Green
    }

    Write-Host "  Checking for existing Service Principal..." -ForegroundColor Yellow
    $existingSp = az ad sp list --filter "appId eq '$ClientId'" --query "[0]" 2>$null | ConvertFrom-Json

    if ($existingSp) {
        Write-Host "  Service Principal already exists" -ForegroundColor Green
        $SpObjectId = $existingSp.id
    }
    else {
        Write-Host "  Creating Service Principal..." -ForegroundColor Yellow
        $sp = az ad sp create --id $ClientId | ConvertFrom-Json
        $SpObjectId = $sp.id
        Write-Host "  Created Service Principal" -ForegroundColor Green
    }

    Write-Host "  Checking role assignments..." -ForegroundColor Yellow
    foreach ($role in $Roles) {
        $existingRole = az role assignment list --assignee $ClientId --role $role --scope "/subscriptions/$SubscriptionId" 2>$null | ConvertFrom-Json

        if ($existingRole -and $existingRole.Count -gt 0) {
            Write-Host "  '$role' role already assigned" -ForegroundColor Green
        }
        else {
            Write-Host "  Assigning '$role' role..." -ForegroundColor Yellow
            az role assignment create `
                --assignee $ClientId `
                --role $role `
                --scope "/subscriptions/$SubscriptionId" | Out-Null
            Write-Host "  Assigned '$role' role" -ForegroundColor Green
        }
    }

    Write-Host "  Configuring federated credentials..." -ForegroundColor Yellow

    # Environment credential
    $envCredentialName = "github-actions-$Environment"
    $envSubject = "repo:${GitHubOwner}/${GitHubRepo}:environment:${Environment}"
    $existingEnvCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$envCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingEnvCred -and $existingEnvCred.Count -gt 0) {
        Write-Host "  Federated credential for environment '$Environment' already exists" -ForegroundColor Green
    }
    else {
        $envCredential = @{
            name        = $envCredentialName
            issuer      = "https://token.actions.githubusercontent.com"
            subject     = $envSubject
            audiences   = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for $Environment environment"
        } | ConvertTo-Json -Compress

        $envCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "  Created federated credential for environment '$Environment'" -ForegroundColor Green
    }

    # Branch credential
    $branchCredentialName = "github-actions-branch-$Branch"
    $branchSubject = "repo:${GitHubOwner}/${GitHubRepo}:ref:refs/heads/${Branch}"
    $existingBranchCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$branchCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingBranchCred -and $existingBranchCred.Count -gt 0) {
        Write-Host "  Federated credential for branch '$Branch' already exists" -ForegroundColor Green
    }
    else {
        $branchCredential = @{
            name        = $branchCredentialName
            issuer      = "https://token.actions.githubusercontent.com"
            subject     = $branchSubject
            audiences   = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for $Branch branch"
        } | ConvertTo-Json -Compress

        $branchCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "  Created federated credential for branch '$Branch'" -ForegroundColor Green
    }

    # Pull request credential
    $prCredentialName = "github-actions-pull-request"
    $prSubject = "repo:${GitHubOwner}/${GitHubRepo}:pull_request"
    $existingPrCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$prCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingPrCred -and $existingPrCred.Count -gt 0) {
        Write-Host "  Federated credential for pull requests already exists" -ForegroundColor Green
    }
    else {
        $prCredential = @{
            name        = $prCredentialName
            issuer      = "https://token.actions.githubusercontent.com"
            subject     = $prSubject
            audiences   = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for pull requests"
        } | ConvertTo-Json -Compress

        $prCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "  Created federated credential for pull requests" -ForegroundColor Green
    }

    return @{
        ClientId    = $ClientId
        DisplayName = $Name
        AppObjectId = $AppObjectId
        SpObjectId  = $SpObjectId
    }
}

# ============================================================
# Main Script
# ============================================================

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  SimplyBudget - Azure OIDC Setup" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# Step 1: Check Prerequisites
# ============================================================

Write-Host "Checking prerequisites..." -ForegroundColor Yellow

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is not installed. Install from https://docs.microsoft.com/cli/azure/install-azure-cli"
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI is not installed. Install from https://cli.github.com/"
}

$azAccount = az account show 2>$null | ConvertFrom-Json
if (-not $azAccount) {
    Write-Host "Not logged into Azure CLI. Please log in..." -ForegroundColor Yellow
    az login
    $azAccount = az account show | ConvertFrom-Json
}
Write-Host "  Azure: Logged in as $($azAccount.user.name)" -ForegroundColor Green

$ghAuth = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Not logged into GitHub CLI. Please log in..." -ForegroundColor Yellow
    gh auth login
}
Write-Host "  GitHub: Authenticated" -ForegroundColor Green

# ============================================================
# SWA Token-only mode
# ============================================================

if ($SetSwaToken) {
    Write-Host ""
    Write-Host "------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "  Setting SWA_DEPLOYMENT_TOKEN" -ForegroundColor Cyan
    Write-Host "------------------------------------------------------" -ForegroundColor Cyan

    Write-Host "  Fetching deployment token for '$SwaName'..." -ForegroundColor Yellow
    $swaToken = az staticwebapp secrets list --name $SwaName --resource-group $SwaResourceGroup --query "properties.apiKey" -o tsv 2>$null

    if (-not $swaToken) {
        throw "Could not fetch SWA deployment token. Ensure '$SwaName' exists in resource group '$SwaResourceGroup' and you have access."
    }

    gh secret set SWA_DEPLOYMENT_TOKEN --repo "$GitHubOwner/$GitHubRepo" --body $swaToken
    Write-Host "  SWA_DEPLOYMENT_TOKEN set" -ForegroundColor Green
    exit 0
}

# ============================================================
# Step 2: Gather Configuration
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Configuration" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host ""

if ($NonInteractive) {
    if (-not $GitHubOwner -or -not $GitHubRepo) {
        throw "GitHubOwner and GitHubRepo are required in non-interactive mode"
    }
}
else {
    $ProjectName = Prompt-WithDefault -Message "Project name" -Default $ProjectName
    $GitHubOwner = Prompt-WithDefault -Message "GitHub owner (org or user)" -Default $GitHubOwner
    $GitHubRepo  = Prompt-WithDefault -Message "GitHub repository name" -Default $GitHubRepo

    if (-not $SubscriptionId) {
        $useCurrentSub = Prompt-YesNo -Message "Use current Azure subscription '$($azAccount.name)' ($($azAccount.id))?" -Default $true
        if (-not $useCurrentSub) {
            $SubscriptionId = Prompt-WithDefault -Message "Azure Subscription ID" -Default ""
        }
    }

    $Environment = Prompt-WithDefault -Message "GitHub environment name" -Default $Environment
    $Branch      = Prompt-WithDefault -Message "Default branch" -Default $Branch
}

if ($SubscriptionId) {
    az account set --subscription $SubscriptionId
    $azAccount = az account show | ConvertFrom-Json
}

$SubscriptionId = $azAccount.id
$TenantId       = $azAccount.tenantId
$AppName        = "${ProjectName}-GitHubActions"

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Configuration Summary" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Project Name:       $ProjectName" -ForegroundColor White
Write-Host "  GitHub:             $GitHubOwner/$GitHubRepo" -ForegroundColor White
Write-Host "  Azure Subscription: $($azAccount.name) ($SubscriptionId)" -ForegroundColor White
Write-Host "  Tenant ID:          $TenantId" -ForegroundColor White
Write-Host "  App Registration:   $AppName" -ForegroundColor White
Write-Host "  Environment:        $Environment" -ForegroundColor White
Write-Host "  Branch:             $Branch" -ForegroundColor White
Write-Host ""

if (-not $NonInteractive) {
    $proceed = Prompt-YesNo -Message "Proceed with this configuration?" -Default $true
    if (-not $proceed) {
        Write-Host "Setup cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# ============================================================
# Step 3: Create App Registration
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 1/3: Creating App Registration" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

$mainApp = Create-AppRegistrationWithRoles `
    -Name $AppName `
    -SubscriptionId $SubscriptionId `
    -GitHubOwner $GitHubOwner `
    -GitHubRepo $GitHubRepo `
    -Environment $Environment `
    -Branch $Branch `
    -Roles @("Contributor", "User Access Administrator")

# ============================================================
# Step 4: Generate Terraform variables file
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 2/3: Generating Terraform Variables" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

$infraDir     = Join-Path $PSScriptRoot "Infra"
$tfvarsPath   = Join-Path $infraDir "azure.auto.tfvars"
$tfvarsContent = @"
SUBSCRIPTION_ID = "$SubscriptionId"
"@
Set-Content -Path $tfvarsPath -Value $tfvarsContent -Encoding UTF8
Write-Host "  Generated $tfvarsPath" -ForegroundColor Green

# ============================================================
# Step 5: Configure GitHub Secrets
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 3/3: Configuring GitHub Secrets" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

Write-Host "  Setting AZURE_CLIENT_ID..." -ForegroundColor Yellow
gh secret set AZURE_CLIENT_ID --repo "$GitHubOwner/$GitHubRepo" --body $mainApp.ClientId

Write-Host "  Setting AZURE_TENANT_ID..." -ForegroundColor Yellow
gh secret set AZURE_TENANT_ID --repo "$GitHubOwner/$GitHubRepo" --body $TenantId

Write-Host "  Setting AZURE_SUBSCRIPTION_ID..." -ForegroundColor Yellow
gh secret set AZURE_SUBSCRIPTION_ID --repo "$GitHubOwner/$GitHubRepo" --body $SubscriptionId

# Try to fetch SWA token if the SWA already exists
Write-Host "  Checking for SWA deployment token..." -ForegroundColor Yellow
$swaToken = az staticwebapp secrets list --name $SwaName --resource-group $SwaResourceGroup --query "properties.apiKey" -o tsv 2>$null

if ($swaToken) {
    Write-Host "  Setting SWA_DEPLOYMENT_TOKEN..." -ForegroundColor Yellow
    gh secret set SWA_DEPLOYMENT_TOKEN --repo "$GitHubOwner/$GitHubRepo" --body $swaToken
    Write-Host "  SWA_DEPLOYMENT_TOKEN set" -ForegroundColor Green
}
else {
    Write-Host "  Static Web App '$SwaName' not found yet - skipping SWA_DEPLOYMENT_TOKEN." -ForegroundColor Yellow
    Write-Host "  After running 'terraform apply', set it with:" -ForegroundColor Yellow
    Write-Host "    .\Setup.ps1 -SetSwaToken" -ForegroundColor Cyan
}

Write-Host "  GitHub secrets configured" -ForegroundColor Green

# ============================================================
# Summary
# ============================================================

Write-Host ""
Write-Host "======================================================" -ForegroundColor Green
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  App Registration:" -ForegroundColor Yellow
Write-Host "    $AppName (Client ID: $($mainApp.ClientId))" -ForegroundColor White
Write-Host ""
Write-Host "  GitHub Secrets set:" -ForegroundColor Yellow
Write-Host "    AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID" -ForegroundColor White
if ($swaToken) {
    Write-Host "    SWA_DEPLOYMENT_TOKEN" -ForegroundColor White
}
Write-Host ""
Write-Host "  Federated Credentials configured for:" -ForegroundColor Yellow
Write-Host "    Environment: $Environment" -ForegroundColor White
Write-Host "    Branch:      $Branch" -ForegroundColor White
Write-Host "    Pull Requests" -ForegroundColor White
Write-Host ""
Write-Host "  Next Steps:" -ForegroundColor Yellow
Write-Host "    1. Ensure the GitHub environment '$Environment' exists:" -ForegroundColor White
Write-Host "       https://github.com/$GitHubOwner/$GitHubRepo/settings/environments" -ForegroundColor Cyan
Write-Host "    2. Apply Terraform infrastructure:" -ForegroundColor White
Write-Host "       cd Infra && terraform init && terraform apply" -ForegroundColor Cyan
if (-not $swaToken) {
    Write-Host "    3. Set the SWA deployment token:" -ForegroundColor White
    Write-Host "       .\Setup.ps1 -SetSwaToken" -ForegroundColor Cyan
}
Write-Host ""
