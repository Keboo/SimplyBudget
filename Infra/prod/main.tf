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

  base_database_connection_string = "Server=tcp:${data.azurerm_mssql_server.existing.fully_qualified_domain_name},1433;Initial Catalog=${data.azurerm_mssql_database.existing.name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;"
  database_connection_string      = "${local.base_database_connection_string}Authentication=\"Active Directory Default\";"
  db_permissions = [
    "db_datareader",
    "db_datawriter",
    "db_ddladmin"
  ]
}

data "azurerm_resource_group" "resource_group" {
  name = var.existing_resource_group_name
}

data "azurerm_client_config" "current" {}

data "azuread_service_principal" "provisioning_principal" {
  client_id = var.provisioning_client_id
}

resource "azurerm_user_assigned_identity" "app_identity" {
  name                = "simplybudget-${lower(local.environment)}-mi"
  location            = data.azurerm_resource_group.resource_group.location
  resource_group_name = data.azurerm_resource_group.resource_group.name

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
  triggers_replace = [
    data.azurerm_mssql_database.existing.id,
    azurerm_user_assigned_identity.app_identity.principal_id,
    azurerm_user_assigned_identity.app_identity.name,
    local.database_schema_name,
    join(",", local.db_permissions),
    var.provisioning_client_id,
    "v1"
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

        Start-Sleep -Seconds 5

        $provisioningPrincipalName = '${data.azuread_service_principal.provisioning_principal.display_name}'
        $provisioningPrincipalObjectId = '${data.azuread_service_principal.provisioning_principal.object_id}'

        $currentAdminObjectId = az sql server ad-admin list `
          --resource-group '${data.azurerm_resource_group.resource_group.name}' `
          --server '${local.sql_server_name}' `
          --query "[0].sid" `
          -o tsv `
          --only-show-errors 2>$null

        $currentAdminObjectId = "$currentAdminObjectId".Trim()
        if (-not $currentAdminObjectId -or $currentAdminObjectId -ne $provisioningPrincipalObjectId) {
          $adminOutput = az sql server ad-admin create `
            --resource-group '${data.azurerm_resource_group.resource_group.name}' `
            --server '${local.sql_server_name}' `
            --display-name $provisioningPrincipalName `
            --object-id $provisioningPrincipalObjectId `
            --only-show-errors 2>&1

          if ($LASTEXITCODE -ne 0) {
            throw "Failed to set SQL Entra admin. Azure CLI output: $adminOutput"
          }

          Start-Sleep -Seconds 20
        }

        $tokenOutput = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv 2>&1
        $token = "$tokenOutput".Trim()
        if ($LASTEXITCODE -ne 0 -or -not $token) {
          throw "Failed to acquire access token for SQL database. Azure CLI output: $tokenOutput"
        }

        $identityName = '${azurerm_user_assigned_identity.app_identity.name}'
        $identityObjectId = '${azurerm_user_assigned_identity.app_identity.principal_id}'
        $schemaName = '${local.database_schema_name}'

        $queryParts = @(
          "IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '$schemaName') BEGIN EXEC('CREATE SCHEMA [$schemaName]'); END;",
          "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$identityName') BEGIN CREATE USER [$identityName] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '$identityObjectId'; END;",
          "ALTER USER [$identityName] WITH DEFAULT_SCHEMA = [$schemaName];"
        )

        $roles = ConvertFrom-Json '${jsonencode(local.db_permissions)}'
        foreach ($role in $roles) {
          $queryParts += "IF NOT EXISTS (SELECT 1 FROM sys.database_role_members drm JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id WHERE r.name = '$role' AND m.name = '$identityName') BEGIN ALTER ROLE [$role] ADD MEMBER [$identityName]; END;"
        }

        $queryParts += "GRANT EXECUTE TO [$identityName];"
        $sql = $queryParts -join " "

        Invoke-Sqlcmd -ConnectionString '${local.base_database_connection_string}' -AccessToken $token -Query $sql
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
  resource_group_name             = data.azurerm_resource_group.resource_group.name
  identity_id                     = azurerm_user_assigned_identity.app_identity.id
  container_registry_login_server = data.azurerm_container_registry.existing.login_server

  env_vars = {
    AZURE_CLIENT_ID                       = azurerm_user_assigned_identity.app_identity.client_id
    ConnectionStrings__Database           = local.database_connection_string
    APPLICATIONINSIGHTS_CONNECTION_STRING = module.application_insights.application_insights.connection_string
    AllowedOrigins__0                     = "https://${module.static_web_app.default_host_name}"
  }

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
    name     = data.azurerm_resource_group.resource_group.name
    location = data.azurerm_resource_group.resource_group.location
  }
  sku = {
    tier = "Free"
    size = "Free"
  }

  tags = local.tags
}

module "application_insights" {
  source = "../modules/app_insights"

  environment    = local.environment
  resource_group = data.azurerm_resource_group.resource_group
  tags           = local.tags

  reader_ids = {}
}
