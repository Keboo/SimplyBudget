locals {
  default_tags = {
    "app" = "SimplyBudget"
  }

  location    = "westus2"
  environment = "prod"
}

module "shared" {
  source = "./shared"

  environment = local.environment

  location = local.location
  tags     = local.default_tags

  app_identities = {
    "prod" = module.prod.app_identity.principal_id
  }
}

module "prod" {
  source = "./prod"

  environment = local.environment

  acr_login_server = module.shared.acr_login_server
  location         = local.location
  tags             = local.default_tags
}
