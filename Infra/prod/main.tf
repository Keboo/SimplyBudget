terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

# ─── Existing shared infrastructure (data sources — no creates) ──────────────

data "azurerm_resource_group" "keboodev" {
  name = "keboodev"
}

data "azurerm_container_registry" "acr" {
  name                = "keboodevacr"
  resource_group_name = data.azurerm_resource_group.keboodev.name
}

data "azurerm_container_app_environment" "env" {
  name                = "keboodev-env"
  resource_group_name = data.azurerm_resource_group.keboodev.name
}

data "azurerm_mssql_server" "db_server" {
  name                = "keboodevdb"
  resource_group_name = data.azurerm_resource_group.keboodev.name
}

# ─── New SimplyBudget resource group ─────────────────────────────────────────

resource "azurerm_resource_group" "main" {
  name     = "SimplyBudget"
  location = var.location
}

# ─── Managed identity for the Container App ──────────────────────────────────

resource "azurerm_user_assigned_identity" "api" {
  name                = "simplybudget-api-identity"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
}

# Grant AcrPull on existing ACR
resource "azurerm_role_assignment" "acr_pull" {
  scope                = data.azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.api.principal_id
}

# ─── SQL Database on existing server ─────────────────────────────────────────

resource "azurerm_mssql_database" "simplybudget" {
  name      = "SimplyBudget"
  server_id = data.azurerm_mssql_server.db_server.id
  sku_name  = "S0"
}

# ─── Application Insights ────────────────────────────────────────────────────

resource "azurerm_log_analytics_workspace" "main" {
  name                = "simplybudget-logs"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"
}

resource "azurerm_application_insights" "main" {
  name                = "simplybudget-appinsights"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
}

# ─── Static Web App (React frontend) ─────────────────────────────────────────

resource "azurerm_static_web_app" "frontend" {
  name                = "simplybudget-swa"
  resource_group_name = azurerm_resource_group.main.name
  location            = "eastus2" # SWA has limited regions; eastus2 is commonly available
  sku_tier            = "Free"
  sku_size            = "Free"
}

# ─── Container App (ASP.NET Core API backend) ────────────────────────────────

locals {
  connection_string = "Server=tcp:${data.azurerm_mssql_server.db_server.fully_qualified_domain_name},1433;Initial Catalog=SimplyBudget;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  api_image         = "${data.azurerm_container_registry.acr.login_server}/simplybudget-api:${var.api_image_tag}"
}

resource "azurerm_container_app" "api" {
  name                         = "simplybudget-api"
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = data.azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  registry {
    server   = data.azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.api.id
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "simplybudget-api"
      image  = local.api_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "ConnectionStrings__Database"
        value = local.connection_string
      }
      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = azurerm_application_insights.main.connection_string
      }
      env {
        name  = "AllowedOrigins__0"
        value = "https://${azurerm_static_web_app.frontend.default_host_name}"
      }
      env {
        name  = "RunMigrationsOnStartup"
        value = tostring(var.run_migrations_on_startup)
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  depends_on = [azurerm_role_assignment.acr_pull, azurerm_mssql_database.simplybudget]
}
