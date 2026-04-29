terraform {
  required_version = ">= 1.6"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azapi = {
      source  = "azure/azapi"
      version = "~> 1.0"
    }
  }
  backend "azurerm" {
    resource_group_name  = "terraform-state"
    storage_account_name = "kebooterraformstate"
    container_name       = "tfstate"
    key                  = "simplybudget.tfstate"
  }
}

provider "azurerm" {
  features {}
}

provider "azapi" {}
