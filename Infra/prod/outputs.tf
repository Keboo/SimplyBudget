output "api_container_app_url" {
  description = "FQDN of the SimplyBudget API Container App"
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}

output "swa_url" {
  description = "URL of the Static Web App"
  value       = "https://${azurerm_static_web_app.frontend.default_host_name}"
}

output "app_insights_connection_string" {
  description = "Application Insights connection string"
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}
