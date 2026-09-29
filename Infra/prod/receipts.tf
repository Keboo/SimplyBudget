resource "random_string" "receipt_storage_suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_storage_account" "receipts" {
  name                            = "sb${substr(replace(lower(local.environment), "/[^a-z0-9]/", ""), 0, 5)}receipt${random_string.receipt_storage_suffix.result}"
  resource_group_name             = azurerm_resource_group.app.name
  location                        = azurerm_resource_group.app.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  https_traffic_only_enabled      = true
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = false

  blob_properties {
    versioning_enabled = true
  }

  tags = local.tags
}

resource "azurerm_storage_container" "receipts" {
  name                  = "receipts"
  storage_account_id    = azurerm_storage_account.receipts.id
  container_access_type = "private"
}

resource "azurerm_role_assignment" "app_identity_receipt_storage" {
  scope                = azurerm_storage_account.receipts.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}

resource "azurerm_cognitive_account" "receipt_document_intelligence" {
  name                  = "simplybudget-${lower(local.environment)}-receipt-${random_string.receipt_storage_suffix.result}"
  location              = azurerm_resource_group.app.location
  resource_group_name   = azurerm_resource_group.app.name
  kind                  = "FormRecognizer"
  sku_name              = "S0"
  custom_subdomain_name = "simplybudget-${lower(local.environment)}-receipt-${random_string.receipt_storage_suffix.result}"

  tags = local.tags
}

resource "azurerm_role_assignment" "app_identity_receipt_document_intelligence" {
  scope                = azurerm_cognitive_account.receipt_document_intelligence.id
  role_definition_name = "Cognitive Services Data Reader"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}
