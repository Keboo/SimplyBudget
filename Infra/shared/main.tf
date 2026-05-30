locals {
  tags = merge(var.tags,
    {
      "Environment" = "Shared"
  })
}

# Reference the existing shared resource group
data "azurerm_resource_group" "shared_rg" {
  name = "KebooDev"
}

# Reference the existing Container Registry
data "azurerm_container_registry" "acr" {
  name                = "keboodevacr"
  resource_group_name = data.azurerm_resource_group.shared_rg.name
}

# Grant AcrPull to each app identity
resource "azurerm_role_assignment" "acr_pull" {
  for_each             = var.app_identities
  scope                = data.azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = each.value
}

