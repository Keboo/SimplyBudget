variable "CLIENT_ID" {
  description = "Value of the client id of the service principal"
  type        = string
  default     = ""
}

variable "MIGRATION_CLIENT_ID" {
  description = "Client ID of the service principal used by the application build/deploy pipeline to run EF Core migrations against the database via Azure AD authentication."
  type        = string
  default     = ""
}

variable "TENANT_ID" {
  type        = string
  description = "Value of the tenant id of the service principal"
  default     = ""
}

variable "SUBSCRIPTION_ID" {
  type        = string
  description = "Value of the subscription id to use"
  default     = ""
}

variable "location" {
  description = "Azure region for the deployment resources."
  type        = string
  default     = "westus2"
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "prod"
}

variable "existing_resource_group_name" {
  description = "Existing resource group that contains shared infrastructure."
  type        = string
  default     = "KebooDev"
}

variable "app_resource_group_name" {
  description = "Name of the dedicated resource group created for SimplyBudget's non-shared infrastructure."
  type        = string
  default     = "SimplyBudget"
}

variable "existing_container_registry_name" {
  description = "Existing Azure Container Registry name."
  type        = string
  default     = "keboodevacr"
}

variable "existing_container_app_environment_name" {
  description = "Existing Azure Container Apps environment name."
  type        = string
  default     = "keboodev-env"
}

variable "existing_sql_server_name" {
  description = "Existing Azure SQL Server name."
  type        = string
  default     = "keboodev-sql"
}

variable "existing_sql_database_name" {
  description = "Existing Azure SQL Database name."
  type        = string
  default     = "keboodevdb"
}

variable "database_schema_name" {
  description = "Default SQL schema used by the app and managed identity."
  type        = string
  default     = "SimplyBudget"
}

variable "sql_admin_group_name" {
  description = "Display name of the existing Entra ID (Azure AD) group configured as the SQL Server's Azure AD administrator."
  type        = string
  default     = "KebooDevDBAdmins"
}
