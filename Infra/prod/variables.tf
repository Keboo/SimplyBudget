variable "environment" {
  description = "The deployment environment (e.g., Dev, Prod)"
  type        = string
}

variable "location" {
  description = "Azure region for the resources"
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

variable "existing_resource_group_name" {
  description = "Existing resource group where infrastructure already exists."
  type        = string
}

variable "existing_container_registry_name" {
  description = "Existing Azure Container Registry name."
  type        = string
}

variable "existing_container_app_environment_name" {
  description = "Existing Azure Container Apps environment name."
  type        = string
}

variable "existing_sql_server_name" {
  description = "Existing Azure SQL Server name."
  type        = string
}

variable "existing_sql_database_name" {
  description = "Existing Azure SQL Database name."
  type        = string
}

variable "database_schema_name" {
  description = "Default SQL schema used by the app and managed identity."
  type        = string
}

variable "provisioning_client_id" {
  description = "Client ID of the service principal that runs Terraform apply."
  type        = string
}
