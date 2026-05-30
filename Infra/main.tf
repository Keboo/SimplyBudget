locals {
  default_tags = {
    "app" = "SimplyBudget"
  }
}

module "prod" {
  source = "./prod"

  environment = var.environment
  location    = var.location
  tags        = local.default_tags

  existing_resource_group_name            = var.existing_resource_group_name
  existing_container_registry_name        = var.existing_container_registry_name
  existing_container_app_environment_name = var.existing_container_app_environment_name
  existing_sql_server_name                = var.existing_sql_server_name
  existing_sql_database_name              = var.existing_sql_database_name
  database_schema_name                    = var.database_schema_name
  provisioning_client_id                  = var.CLIENT_ID
}
