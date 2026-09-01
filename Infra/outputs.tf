output "acr_login_server" {
  description = "The login server for the Azure Container Registry"
  value       = module.prod.acr_login_server
}

output "backend_container_app_name" {
  description = "The name of the backend container app"
  value       = module.prod.backend_container_app_name
}

output "backend_container_app_cooldown_period_seconds" {
  description = "Cooldown period for backend container app autoscaling, in seconds"
  value       = module.prod.backend_container_app_cooldown_period_seconds
}

output "resource_group_name" {
  description = "The name of the dedicated SimplyBudget resource group containing the app's non-shared resources"
  value       = module.prod.resource_group_name
}

output "shared_resource_group_name" {
  description = "The name of the existing shared resource group (KebooDev) referenced for shared infrastructure"
  value       = module.prod.shared_resource_group_name
}

output "static_web_app_name" {
  description = "The name of the static web app"
  value       = module.prod.static_web_app_name
}

output "static_web_app_api_key" {
  description = "The API key for the static web app deployment"
  value       = module.prod.static_web_app_api_key
  sensitive   = true
}

output "static_web_app_url" {
  description = "The URL of the deployed static web app"
  value       = module.prod.static_web_app_url
}

output "backend_url" {
  description = "The URL of the backend API"
  value       = module.prod.backend_url
}

output "applicationinsights_connection_string" {
  description = "The connection string for Application Insights"
  value       = module.prod.applicationinsights_connection_string
  sensitive   = true
}

output "database_connection_string" {
  description = "The connection string for the SQL database"
  value       = module.prod.database_connection_string
  sensitive   = true
}

output "entra_client_id" {
  description = "Client (application) ID of the Entra ID App Registration used for MSAL sign-in and API authorization"
  value       = module.prod.entra_client_id
}

output "entra_tenant_id" {
  description = "Entra ID (Azure AD) tenant ID that the application is registered in"
  value       = module.prod.entra_tenant_id
}
