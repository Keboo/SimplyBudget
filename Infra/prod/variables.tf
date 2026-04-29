variable "location" {
  description = "Azure region for all new SimplyBudget resources"
  type        = string
  default     = "eastus"
}

variable "api_image_tag" {
  description = "Docker image tag to deploy to the Container App"
  type        = string
  default     = "latest"
}

variable "sql_admin_password" {
  description = "Password for the SQL Server admin login (existing server)"
  type        = string
  sensitive   = true
}

variable "run_migrations_on_startup" {
  description = "Whether the API should run EF migrations on startup"
  type        = bool
  default     = true
}
