
// TODO (future - not currently available):
// Automate assignment of custom domain and SSL cert
// https://github.com/microsoft/azure-container-apps/issues/796#issuecomment-2515167794 
// https://github.com/hashicorp/terraform-provider-azurerm/pull/31137 

resource "azurerm_container_app" "app" {
  name                         = var.name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"
  tags                         = var.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [var.identity_id]
  }

  registry {
    server   = var.container_registry_login_server
    identity = var.identity_id
  }

  template {
    min_replicas               = var.min_replicas
    max_replicas               = var.max_replicas
    cooldown_period_in_seconds = var.cooldown_period_seconds

    container {
      name   = var.name
      image  = "${var.container_registry_login_server}/crccheck/hello-world:latest"
      cpu    = var.cpu
      memory = var.memory

      env {
        name  = "PORT"
        value = "8080"
      }

      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080;8081"
      }

      env {
        name  = "HEALTH_PORT"
        value = "8081"
      }

      dynamic "env" {
        for_each = var.env_vars
        content {
          name  = env.key
          value = env.value
        }
      }

      liveness_probe {
        path             = "/alive"
        port             = 8081
        transport        = "HTTP"
        initial_delay    = 10
        interval_seconds = 30
      }

      readiness_probe {
        path             = "/alive"
        port             = 8081
        transport        = "HTTP"
        interval_seconds = 2
      }

      # The startup probe gates how quickly a cold (scaled-from-zero) replica can start
      # serving. A long interval adds pure latency even when the app is ready immediately, so
      # poll frequently and get the failure budget from the threshold instead.
      # "/health" must stay cheap and dependency-free (no database check) - adding a
      # dependency check here would put Azure SQL resume time on the critical startup path.
      startup_probe {
        path                    = "/health"
        port                    = 8081
        transport               = "HTTP"
        interval_seconds        = 2
        failure_count_threshold = 40 # 2 * 40 = 80 seconds
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
    ]
  }
}
