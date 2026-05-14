output "acr_login_server" {
  description = "The login server for the Azure Container Registry"
  value       = module.shared.acr_login_server
}

output "backend_container_app_name" {
  description = "The name of the backend container app"
  value       = module.prod.backend_container_app_name
}

output "database_connection_string" {
  description = "The connection string for the SQL database"
  value       = module.prod.database_connection_string
}

output "resource_group_name" {
  description = "The name of the resource group"
  value       = module.prod.resource_group_name
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
