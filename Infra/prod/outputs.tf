output "app_identity" {
  value = azurerm_user_assigned_identity.app_identity
}

output "backend_container_app_name" {
  description = "The name of the backend container app"
  value       = module.backend_container_app.name
}

output "resource_group_name" {
  description = "The name of the resource group"
  value       = data.azurerm_resource_group.resource_group.name
}

output "database_connection_string" {
  description = "The connection string for the SQL database (uses managed identity auth)"
  value       = "Server=tcp:${data.azurerm_mssql_server.sql_server.fully_qualified_domain_name},1433;Database=${data.azurerm_mssql_database.db.name};Authentication=Active Directory Managed Identity;Encrypt=True;"
}

output "backend_url" {
  description = "The URL of the backend API"
  value       = "https://${module.backend_container_app.fqdn}"
}

output "applicationinsights_connection_string" {
  description = "The connection string for Application Insights"
  value       = module.application_insights.application_insights.connection_string
  sensitive   = true
}
