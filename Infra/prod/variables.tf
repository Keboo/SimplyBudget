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
  description = "Existing resource group where shared infrastructure already exists."
  type        = string
}

variable "app_resource_group_name" {
  description = "Name of the dedicated resource group created for SimplyBudget's non-shared infrastructure."
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

variable "migration_client_id" {
  description = "Client ID of the service principal used by the application build/deploy pipeline to run EF Core migrations against the database via Azure AD authentication."
  type        = string
}

variable "sql_admin_group_name" {
  description = "Display name of the existing Entra ID (Azure AD) group configured as the SQL Server's Azure AD administrator. Members of this group (including the Terraform and migration service principals) receive administrative access to the database."
  type        = string
}

variable "app_users_group_name" {
  description = "Display name of the existing Entra ID (Azure AD) security group whose members are authorized to use the SimplyBudget application. The backend authorizes requests by checking the caller's 'groups' claim against this group's object ID."
  type        = string
  default     = "SimplyBudgetUsers"
}

variable "database_admin_user_names" {
  description = "UPNs/email addresses of individual Entra ID (Azure AD) users who should be explicitly created as database users with db_owner rights. Even though members of the sql_admin_group already have administrative access via the server's Azure AD admin, the Azure Portal's Query Editor does not reliably resolve group-based admin membership, so an explicit contained user is created for each of these individuals."
  type        = list(string)
  default     = []
}

variable "entra_web_app_client_id" {
  description = "Client (application) ID of the existing Entra ID App Registration used for end-user sign-in (MSAL SPA) and API authorization (exposes the 'access_as_user' scope). Managed outside of Terraform."
  type        = string
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
