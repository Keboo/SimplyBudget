output "app_identity" {
  value = azurerm_user_assigned_identity.app_identity
}

output "acr_login_server" {
  description = "The login server for the Azure Container Registry"
  value       = data.azurerm_container_registry.existing.login_server
}

output "backend_container_app_name" {
  description = "The name of the backend container app"
  value       = module.backend_container_app.name
}

output "resource_group_name" {
  description = "The name of the dedicated SimplyBudget resource group containing the app's non-shared resources"
  value       = azurerm_resource_group.app.name
}

output "shared_resource_group_name" {
  description = "The name of the existing shared resource group (KebooDev) referenced for shared infrastructure"
  value       = data.azurerm_resource_group.resource_group.name
}

output "database_connection_string" {
  description = "The connection string for the SQL database"
  value       = local.database_connection_string
  sensitive   = true
}

output "static_web_app_name" {
  description = "The name of the static web app"
  value       = module.static_web_app.name
}

output "static_web_app_api_key" {
  description = "The API key for the static web app deployment"
  value       = module.static_web_app.api_key
  sensitive   = true
}

output "static_web_app_url" {
  description = "The URL of the deployed static web app"
  value       = "https://${module.static_web_app.default_host_name}"
}

output "backend_url" {
  description = "The URL of the backend API. Uses the custom domain when var.backend_custom_domain is non-empty, otherwise falls back to the auto-generated Container App FQDN."
  value       = var.backend_custom_domain != "" ? var.backend_custom_domain : "https://${module.backend_container_app.fqdn}"
}

output "applicationinsights_connection_string" {
  description = "The connection string for Application Insights"
  value       = module.application_insights.application_insights.connection_string
  sensitive   = true
}

output "entra_client_id" {
  description = "Client (application) ID of the Entra ID App Registration used for MSAL sign-in and API authorization"
  value       = data.azuread_application.webapp.client_id
}

output "entra_tenant_id" {
  description = "Entra ID (Azure AD) tenant ID that the application is registered in"
  value       = data.azurerm_client_config.current.tenant_id
}

output "entra_application_id" {
  description = "Terraform resource ID (\"/applications/{object-id}\") of the Entra ID App Registration, for use in root-module import blocks that target resources inside this module (import blocks are only allowed in the root module)."
  value       = data.azuread_application.webapp.id
}
