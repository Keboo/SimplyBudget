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

variable "app_users_group_name" {
  description = "Display name of the existing Entra ID (Azure AD) security group whose members are authorized to use the SimplyBudget application."
  type        = string
  default     = "SimplyBudgetUsers"
}

variable "database_admin_user_names" {
  description = "UPNs/email addresses of individual Entra ID (Azure AD) users who should be explicitly created as database users with db_owner rights. Even though members of the sql_admin_group already have administrative access via the server's Azure AD admin, the Azure Portal's Query Editor does not reliably resolve group-based admin membership, so an explicit contained user is created for each of these individuals."
  type        = list(string)
  default     = ["kitokeboo@gmail.com"]
}

variable "entra_web_app_client_id" {
  description = "Client (application) ID of the existing Entra ID App Registration used for end-user sign-in (MSAL SPA) and API authorization. Managed outside of Terraform."
  type        = string
  default     = "c0f5bb7e-cf2d-436f-b0a6-934dcec490b4"
}

variable "backend_custom_domain" {
  description = "Custom domain URL for the backend API (e.g. https://api.budget.keboo.dev). When set, overrides the auto-generated Container App FQDN as the backend_url output and as the allowed CORS origin on the backend."
  type        = string
  default     = ""
}

variable "frontend_custom_domain" {
  description = "Custom domain URL for the frontend Static Web App (e.g. https://budget.keboo.dev). When set, it is added to the Entra ID App Registration's SPA redirect URIs so MSAL sign-in redirects succeed on the custom domain."
  type        = string
  default     = ""
}
