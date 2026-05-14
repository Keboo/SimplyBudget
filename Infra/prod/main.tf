locals {
  environment = var.environment
  tags = merge(var.tags,
    {
      "Environment" = local.environment
  })
}

# Use the existing KebooDev resource group
data "azurerm_resource_group" "resource_group" {
  name = "KebooDev"
}

# Managed identity for the container app
resource "azurerm_user_assigned_identity" "app_identity" {
  name                = "simplybudget-web-mi"
  location            = data.azurerm_resource_group.resource_group.location
  resource_group_name = data.azurerm_resource_group.resource_group.name

  tags = local.tags
}

# Reference the existing Container App Environment
data "azurerm_container_app_environment" "cae" {
  name                = "keboodev-env"
  resource_group_name = data.azurerm_resource_group.resource_group.name
}

# Reference the existing SQL Server
data "azurerm_mssql_server" "sql_server" {
  name                = "keboodev-sql"
  resource_group_name = data.azurerm_resource_group.resource_group.name
}

# Reference the existing SQL Database
data "azurerm_mssql_database" "db" {
  name      = "keboodevdb"
  server_id = data.azurerm_mssql_server.sql_server.id
}

module "backend_container_app" {
  source = "../modules/container_app"

  name                            = "simplybudget-web-backend"
  container_app_environment_id    = data.azurerm_container_app_environment.cae.id
  resource_group_name             = data.azurerm_resource_group.resource_group.name
  identity_id                     = azurerm_user_assigned_identity.app_identity.id
  container_registry_login_server = var.acr_login_server

  env_vars = {
    AZURE_CLIENT_ID = azurerm_user_assigned_identity.app_identity.client_id
    # Aspire uses ConnectionStrings__<key> naming convention.
    # Authentication uses the managed identity (AZURE_CLIENT_ID sets the client to use).
    ConnectionStrings__Database           = "Server=tcp:${data.azurerm_mssql_server.sql_server.fully_qualified_domain_name},1433;Database=${data.azurerm_mssql_database.db.name};Authentication=Active Directory Managed Identity;Encrypt=True;"
    APPLICATIONINSIGHTS_CONNECTION_STRING = module.application_insights.application_insights.connection_string
    AllowedOrigins__0                     = "https://${module.backend_container_app.fqdn}"
  }

  depends_on = [module.application_insights]
}

module "application_insights" {
  source = "../modules/app_insights"

  environment    = local.environment
  resource_group = data.azurerm_resource_group.resource_group
  tags           = local.tags

  reader_ids = {}
}
