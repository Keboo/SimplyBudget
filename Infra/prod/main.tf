locals {
  environment = var.environment
  tags = merge(var.tags,
    {
      "Environment" = local.environment
  })

  sql_server_name            = var.existing_sql_server_name
  sql_database_name          = var.existing_sql_database_name
  database_schema_name       = var.database_schema_name
  backend_container_app_name = "simplybudget-${lower(local.environment)}-backend"
  static_web_app_name        = "simplybudget-${lower(local.environment)}-swa"

  base_database_connection_string         = "Server=tcp:${data.azurerm_mssql_server.existing.fully_qualified_domain_name},1433;Initial Catalog=${data.azurerm_mssql_database.existing.name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;"
  provisioning_database_connection_string = "Server=tcp:${data.azurerm_mssql_server.existing.fully_qualified_domain_name},1433;Initial Catalog=${data.azurerm_mssql_database.existing.name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=300;"
  database_connection_string              = "${local.base_database_connection_string}Authentication=\"Active Directory Default\";"
  db_permissions = [
    "db_datareader",
    "db_datawriter",
    "db_ddladmin"
  ]
  database_admin_user_names = var.database_admin_user_names
}

# Shared infrastructure (Container App Environment, Container Registry, SQL Server/Database)
# lives in the existing KebooDev resource group and is referenced, not managed, here.
data "azurerm_resource_group" "resource_group" {
  name = var.existing_resource_group_name
}

# Dedicated resource group for SimplyBudget's non-shared infrastructure
# (managed identity, container app, static web app, application insights).
resource "azurerm_resource_group" "app" {
  name     = var.app_resource_group_name
  location = var.location

  tags = local.tags
}

data "azurerm_client_config" "current" {}

data "azuread_service_principal" "provisioning_principal" {
  client_id = var.provisioning_client_id
}

data "azuread_service_principal" "migration_principal" {
  client_id = var.migration_client_id
}

# The Entra ID App Registration used for end-user sign-in (MSAL SPA) and API
# authorization. This app is provisioned and managed outside of Terraform
# (see repository docs), so it is referenced here as a data source rather than
# a managed resource.
data "azuread_application" "webapp" {
  client_id = var.entra_web_app_client_id
}

# Manages the SPA redirect URIs on the webapp App Registration. This is
# authoritative for the SPA redirect URI list (any URI not listed here will be
# removed from the app), so every environment the frontend can be reached from
# (local dev, the SWA's auto-generated default hostname, and the production
# custom domain) must be included. Without the custom domain here, sign-in
# from https://budget.keboo.dev fails with AADSTS50011 (redirect URI mismatch).
resource "azuread_application_redirect_uris" "webapp_spa" {
  application_id = data.azuread_application.webapp.id
  type           = "SPA"

  redirect_uris = compact([
    "http://localhost:5173",
    "https://${module.static_web_app.default_host_name}",
    var.frontend_custom_domain,
  ])
}

# The Entra ID group that is configured as the SQL Server's Azure AD administrator.
# This group is managed outside of Terraform; membership below ensures the
# service principals that need to administer the database (running Terraform
# and applying EF Core migrations) inherit that access.
data "azuread_group" "sql_admins" {
  display_name     = var.sql_admin_group_name
  security_enabled = true
}

# The Entra ID group whose members are authorized to use the application.
# Its object ID is passed to the backend so it can validate the caller's 'groups' claim.
data "azuread_group" "app_users" {
  display_name     = var.app_users_group_name
  security_enabled = true
}

resource "azuread_group_member" "provisioning_principal_sql_admin" {
  group_object_id  = data.azuread_group.sql_admins.object_id
  member_object_id = data.azuread_service_principal.provisioning_principal.object_id
}

resource "azuread_group_member" "migration_principal_sql_admin" {
  group_object_id  = data.azuread_group.sql_admins.object_id
  member_object_id = data.azuread_service_principal.migration_principal.object_id
}

resource "azurerm_user_assigned_identity" "app_identity" {
  name                = "simplybudget-${lower(local.environment)}-mi"
  location            = azurerm_resource_group.app.location
  resource_group_name = azurerm_resource_group.app.name

  tags = local.tags
}

data "azurerm_container_app_environment" "existing" {
  name                = var.existing_container_app_environment_name
  resource_group_name = data.azurerm_resource_group.resource_group.name
}

data "azurerm_container_registry" "existing" {
  name                = var.existing_container_registry_name
  resource_group_name = data.azurerm_resource_group.resource_group.name
}

resource "azurerm_role_assignment" "app_identity_acr_pull" {
  scope                = data.azurerm_container_registry.existing.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}

data "azurerm_mssql_server" "existing" {
  name                = local.sql_server_name
  resource_group_name = data.azurerm_resource_group.resource_group.name
}

data "azurerm_mssql_database" "existing" {
  name      = local.sql_database_name
  server_id = data.azurerm_mssql_server.existing.id
}

resource "terraform_data" "setup_database_principal" {
  depends_on = [
    azuread_group_member.provisioning_principal_sql_admin,
    azuread_group_member.migration_principal_sql_admin
  ]

  triggers_replace = [
    data.azurerm_mssql_database.existing.id,
    azurerm_user_assigned_identity.app_identity.principal_id,
    azurerm_user_assigned_identity.app_identity.client_id,
    azurerm_user_assigned_identity.app_identity.name,
    local.database_schema_name,
    join(",", local.db_permissions),
    var.provisioning_client_id,
    data.azuread_group.sql_admins.object_id,
    join(",", local.database_admin_user_names),
    "v6" # Bumped from v5: treat Azure SQL "database is not currently available"
    # cold-start message as transient so retry/backoff is applied.
    # Bumped from v4: add resilient SQL warm-up/retry logic for free-tier cold starts
    # and use a longer connection timeout for Terraform-driven principal setup.
    # Bumped from v3: repair a stale contained database user left behind
    # when azurerm_user_assigned_identity.app_identity is destroyed and
    # recreated (e.g. by a prior `terraform apply`). Recreating the identity
    # changes its Client ID/Object ID but keeps its name, so the old
    # "IF NOT EXISTS ... WHERE name = ..." guard silently skipped recreating
    # the user, leaving it bound to the deleted identity's SID and causing
    # every DB-backed request to fail with "Login failed for user
    # '<token-identified principal>'" (500s).
    # Bumped from v2: explicitly create contained database users (db_owner) for
    # individual admins, since the Azure Portal Query Editor does not reliably
    # resolve access granted only via the sql_admins group membership.
  ]

  provisioner "local-exec" {
    command = <<-EOT
      $ErrorActionPreference = 'Stop'
      $ipRuleName = $null

      try {
        $currentIp = (Invoke-RestMethod -Uri "https://api.ipify.org").ToString()
        $ruleSuffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
        $ipRuleName = "TerraformTemp-SimplyBudget-$ruleSuffix"

        Install-Module -Name SqlServer -AcceptLicense -Force -ErrorAction SilentlyContinue
        Import-Module SqlServer -ErrorAction Stop

        $firewallOutput = az sql server firewall-rule create `
          --resource-group '${data.azurerm_resource_group.resource_group.name}' `
          --server '${local.sql_server_name}' `
          --name $ipRuleName `
          --start-ip-address $currentIp `
          --end-ip-address $currentIp `
          --only-show-errors 2>&1

        if ($LASTEXITCODE -ne 0) {
          throw "Failed to create firewall rule. Azure CLI output: $firewallOutput"
        }

        # Free-tier SQL can wake up slowly from a cold state; give firewall rules extra time to propagate.
        Start-Sleep -Seconds 15

        $sqlAdminGroupName = '${data.azuread_group.sql_admins.display_name}'
        $sqlAdminGroupObjectId = '${data.azuread_group.sql_admins.object_id}'

        $currentAdminObjectId = az sql server ad-admin list `
          --resource-group '${data.azurerm_resource_group.resource_group.name}' `
          --server '${local.sql_server_name}' `
          --query "[0].sid" `
          -o tsv `
          --only-show-errors 2>$null

        $currentAdminObjectId = "$currentAdminObjectId".Trim()
        if (-not $currentAdminObjectId -or $currentAdminObjectId -ne $sqlAdminGroupObjectId) {
          $adminOutput = az sql server ad-admin create `
            --resource-group '${data.azurerm_resource_group.resource_group.name}' `
            --server '${local.sql_server_name}' `
            --display-name $sqlAdminGroupName `
            --object-id $sqlAdminGroupObjectId `
            --only-show-errors 2>&1

          if ($LASTEXITCODE -ne 0) {
            throw "Failed to set SQL Entra admin. Azure CLI output: $adminOutput"
          }

          Start-Sleep -Seconds 20
        }

        $identityName = '${azurerm_user_assigned_identity.app_identity.name}'
        $identityObjectId = '${azurerm_user_assigned_identity.app_identity.principal_id}'
        $identityClientId = '${azurerm_user_assigned_identity.app_identity.client_id}'
        $schemaName = '${local.database_schema_name}'

        $queryParts = @(
          "IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '$schemaName') BEGIN EXEC('CREATE SCHEMA [$schemaName]'); END;",
          # Azure SQL derives a managed identity's login SID from its Client ID
          # (AppId), not its Object ID/Principal ID. If the identity was
          # destroyed and recreated (e.g. by a prior `terraform apply`), the
          # existing user row still has the old identity's SID even though the
          # name matches, so tokens from the current identity are rejected
          # with "Login failed for user '<token-identified principal>'". Drop
          # the stale user before recreating it so it's rebound to the
          # current Client ID.
          "IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$identityName' AND sid <> CAST(CAST('$identityClientId' AS uniqueidentifier) AS varbinary(16))) BEGIN DROP USER [$identityName]; END;",
          "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$identityName') BEGIN CREATE USER [$identityName] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '$identityObjectId'; END;",
          "ALTER USER [$identityName] WITH DEFAULT_SCHEMA = [$schemaName];"
        )

        $roles = ConvertFrom-Json '${jsonencode(local.db_permissions)}'
        foreach ($role in $roles) {
          $queryParts += "IF NOT EXISTS (SELECT 1 FROM sys.database_role_members drm JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id WHERE r.name = '$role' AND m.name = '$identityName') BEGIN ALTER ROLE [$role] ADD MEMBER [$identityName]; END;"
        }

        $queryParts += "GRANT EXECUTE TO [$identityName];"

        # Explicitly create a contained database user (with db_owner rights) for each
        # individual admin. Members of the sql_admins group already have administrative
        # access via the server's Azure AD admin, but the Azure Portal's Query Editor does
        # not reliably resolve access granted only through group membership, so an explicit
        # user mapping is required for that experience to work.
        $adminUsers = ConvertFrom-Json '${jsonencode(local.database_admin_user_names)}'
        foreach ($adminUser in $adminUsers) {
          $queryParts += "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$adminUser') BEGIN CREATE USER [$adminUser] FROM EXTERNAL PROVIDER; END;"
          $queryParts += "IF NOT EXISTS (SELECT 1 FROM sys.database_role_members drm JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id WHERE r.name = 'db_owner' AND m.name = '$adminUser') BEGIN ALTER ROLE db_owner ADD MEMBER [$adminUser]; END;"
        }

        $sql = $queryParts -join " "

        $maxSqlAttempts = 12
        $retryDelaySeconds = 10
        $maxRetryDelaySeconds = 90
        $sqlConfigured = $false

        function Test-IsTransientSqlStartupError([string]$ErrorMessage) {
          if (-not $ErrorMessage) {
            return $false
          }

          return $ErrorMessage -match '(?i)timeout|timed out|transport-level error|service is currently busy|temporarily unavailable|not currently available|retry the connection later|please retry|please try again|error 40197|error 40501|error 40613|error 49918|error 49919|error 49920|client with ip address|login failed for user ''<token-identified principal>'''
        }

        for ($attempt = 1; $attempt -le $maxSqlAttempts; $attempt++) {
          try {
            $tokenOutput = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv 2>&1
            $token = "$tokenOutput".Trim()
            if ($LASTEXITCODE -ne 0 -or -not $token) {
              throw "Failed to acquire access token for SQL database. Azure CLI output: $tokenOutput"
            }

            # Warm-up probe to handle free-tier cold starts before running principal setup.
            Invoke-Sqlcmd -ConnectionString '${local.provisioning_database_connection_string}' -AccessToken $token -Query "SELECT 1" -QueryTimeout 120

            Invoke-Sqlcmd -ConnectionString '${local.provisioning_database_connection_string}' -AccessToken $token -Query $sql -QueryTimeout 300
            $sqlConfigured = $true
            break
          }
          catch {
            $errorMessage = $_.Exception.Message
            if ($attempt -eq $maxSqlAttempts -or -not (Test-IsTransientSqlStartupError $errorMessage)) {
              throw
            }

            $jitterSeconds = Get-Random -Minimum 0 -Maximum 6
            $sleepSeconds = [Math]::Min(($retryDelaySeconds + $jitterSeconds), $maxRetryDelaySeconds)
            Write-Host "SQL not ready yet (attempt $attempt/$maxSqlAttempts): $errorMessage"
            Write-Host "Retrying in $sleepSeconds seconds..."
            Start-Sleep -Seconds $sleepSeconds
            $retryDelaySeconds = [Math]::Min(($retryDelaySeconds * 2), $maxRetryDelaySeconds)
          }
        }

        if (-not $sqlConfigured) {
          throw "Failed to configure SQL principal after $maxSqlAttempts attempts."
        }
      }
      finally {
        $ErrorActionPreference = 'SilentlyContinue'
        if ($ipRuleName) {
          az sql server firewall-rule delete `
            --resource-group '${data.azurerm_resource_group.resource_group.name}' `
            --server '${local.sql_server_name}' `
            --name $ipRuleName `
            --yes `
            --only-show-errors `
            2>$null
        }
      }

      exit 0
    EOT

    interpreter = ["pwsh", "-Command"]
  }
}

module "backend_container_app" {
  source = "../modules/container_app"

  name                            = local.backend_container_app_name
  container_app_environment_id    = data.azurerm_container_app_environment.existing.id
  resource_group_name             = azurerm_resource_group.app.name
  identity_id                     = azurerm_user_assigned_identity.app_identity.id
  container_registry_login_server = data.azurerm_container_registry.existing.login_server

  # AllowedOrigins must cover every hostname the frontend can be served from
  # (the SWA's auto-generated default hostname and the production custom
  # domain), mirroring the azuread_application_redirect_uris.webapp_spa list
  # above. Without the custom domain here, requests from
  # https://budget.keboo.dev fail CORS with "missing allowed origin".
  env_vars = merge(
    {
      AZURE_CLIENT_ID                         = azurerm_user_assigned_identity.app_identity.client_id
      ConnectionStrings__Database             = local.database_connection_string
      APPLICATIONINSIGHTS_CONNECTION_STRING   = module.application_insights.application_insights.connection_string
      AllowedOrigins__0                       = "https://${module.static_web_app.default_host_name}"
      AzureAd__TenantId                       = data.azurerm_client_config.current.tenant_id
      AzureAd__ClientId                       = data.azuread_application.webapp.client_id
      Authorization__SimplyBudgetUsersGroupId = data.azuread_group.app_users.object_id
    },
    var.frontend_custom_domain != "" ? { AllowedOrigins__1 = var.frontend_custom_domain } : {}
  )

  depends_on = [
    module.application_insights,
    module.static_web_app,
    terraform_data.setup_database_principal
  ]
}

module "static_web_app" {
  source = "../modules/static_web_app"

  name = local.static_web_app_name
  resource_group = {
    name     = azurerm_resource_group.app.name
    location = azurerm_resource_group.app.location
  }
  sku = {
    tier = "Free"
    size = "Free"
  }

  tags = local.tags
}

module "application_insights" {
  source = "../modules/app_insights"

  environment = local.environment
  resource_group = {
    name     = azurerm_resource_group.app.name
    location = azurerm_resource_group.app.location
  }
  tags = local.tags

  reader_ids = {}
}
