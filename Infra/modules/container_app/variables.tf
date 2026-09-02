variable "name" {
  description = "Name of the container app"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}
variable "identity_id" {
  description = "The resource ID of the user-assigned managed identity."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID of the Container App Environment"
  type        = string
}

variable "container_registry_login_server" {
  description = "Container registry server"
  type        = string
}

variable "cpu" {
  description = "CPU cores allocated to the container (e.g. 0.5, 1.0)."
  type        = number
  default     = 0.5
}

variable "memory" {
  description = "Memory allocated to the container (e.g. 1Gi)."
  type        = string
  default     = "1Gi"
}

variable "min_replicas" {
  description = <<-EOT
    Minimum number of replicas.

    Deliberately 0: scale-to-zero is the main cost lever for this low-traffic app, and the
    cooldown period (see cooldown_period_seconds) already keeps the app warm through a normal
    session. The cost of that choice is a cold start after an idle period, which is mitigated in
    the app itself (ReadyToRun publish, chiseled base image, no migrate-on-startup, post-start
    warm-up of the database connection and Entra ID metadata) and by a fast startup probe.

    Set to 1 to eliminate cold starts entirely at the cost of running a replica 24/7.
  EOT
  type        = number
  default     = 0
}

variable "max_replicas" {
  description = "Maximum number of replicas."
  type        = number
  default     = 1
}

variable "cooldown_period_seconds" {
  description = "Seconds to wait after the last active trigger before scaling replicas back down (e.g. to 0). Azure Container Apps defaults to 300 (5 minutes) if unset; max allowed is 3600."
  type        = number
  default     = 300
}

variable "env_vars" {
  description = "Map of environment variables for the container."
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}
