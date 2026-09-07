# ---------------------------------------------------------------------------------------------
# Container Apps
# ---------------------------------------------------------------------------------------------

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${local.name}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.tags
}

resource "azurerm_container_app_environment" "this" {
  name                       = "cae-${local.name}"
  resource_group_name        = azurerm_resource_group.this.name
  location                   = azurerm_resource_group.this.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
  tags                       = local.tags
}

locals {
  postgres_host = azurerm_postgresql_flexible_server.this.fqdn

  connection_string = {
    for database in ["dental", "dental_hangfire_api", "dental_notifications", "dental_hangfire_notifications"] :
    database => "Host=${local.postgres_host};Database=${database};Username=dental;Password=${random_password.postgres.result};SSL Mode=Require;Trust Server Certificate=true"
  }

  redis_connection = "${azurerm_redis_cache.this.hostname}:${azurerm_redis_cache.this.ssl_port},password=${azurerm_redis_cache.this.primary_access_key},ssl=True,abortConnect=False"
}

resource "azurerm_container_app" "api" {
  name                         = "ca-api-${var.environment}"
  resource_group_name          = azurerm_resource_group.this.name
  container_app_environment_id = azurerm_container_app_environment.this.id
  revision_mode                = "Single"
  tags                         = local.tags

  identity {
    type = "SystemAssigned"
  }

  secret {
    name  = "database-connection"
    value = local.connection_string["dental"]
  }

  secret {
    name  = "jobs-connection"
    value = local.connection_string["dental_hangfire_api"]
  }

  secret {
    name  = "redis-connection"
    value = local.redis_connection
  }

  secret {
    name  = "jwt-signing-key"
    value = random_password.jwt_signing_key.result
  }

  secret {
    name  = "storage-access-key"
    value = var.storage_access_key
  }

  secret {
    name  = "storage-secret-key"
    value = var.storage_secret_key
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

  template {
    min_replicas = var.api_min_replicas
    max_replicas = var.api_max_replicas

    container {
      name   = "api"
      image  = var.api_image
      cpu    = 1.0
      memory = "2Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name        = "DatabaseOptions__ConnectionString"
        secret_name = "database-connection"
      }

      env {
        name        = "JobOptions__ConnectionString"
        secret_name = "jobs-connection"
      }

      env {
        name        = "CachingOptions__Redis"
        secret_name = "redis-connection"
      }

      env {
        name        = "JwtOptions__SigningKey"
        secret_name = "jwt-signing-key"
      }

      env {
        name  = "Storage__Provider"
        value = var.storage_service_url == "" ? "local" : "s3"
      }

      env {
        name  = "Storage__ServiceUrl"
        value = var.storage_service_url
      }

      env {
        name  = "Storage__Bucket"
        value = var.storage_bucket
      }

      env {
        name        = "Storage__AccessKey"
        secret_name = "storage-access-key"
      }

      env {
        name        = "Storage__SecretKey"
        secret_name = "storage-secret-key"
      }

      # Liveness must not touch a dependency: a database blip should take a replica out of the load
      # balancer, not restart every replica in the revision.
      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/alive"
      }

      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/ready"
      }
    }

    # Sticky sessions are NOT configured, and must not be: SignalR is backed by the Redis backplane
    # precisely so any replica can serve any connection.
    http_scale_rule {
      name                = "http"
      concurrent_requests = 50
    }
  }

  depends_on = [azurerm_container_app_job.migrator]
}

resource "azurerm_container_app" "notifications" {
  name                         = "ca-notifications-${var.environment}"
  resource_group_name          = azurerm_resource_group.this.name
  container_app_environment_id = azurerm_container_app_environment.this.id
  revision_mode                = "Single"
  tags                         = local.tags

  secret {
    name  = "database-connection"
    value = local.connection_string["dental_notifications"]
  }

  secret {
    name  = "jobs-connection"
    value = local.connection_string["dental_hangfire_notifications"]
  }

  secret {
    name  = "redis-connection"
    value = local.redis_connection
  }

  secret {
    name  = "jwt-signing-key"
    value = random_password.jwt_signing_key.result
  }

  # No ingress: the extracted host is reached only by events. Exposing it would create a second
  # front door onto the same data with none of the API's rate limiting in front of it.
  template {
    min_replicas = 1
    max_replicas = 3

    container {
      name   = "notifications"
      image  = var.notifications_image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name        = "DatabaseOptions__ConnectionString"
        secret_name = "database-connection"
      }

      env {
        name        = "JobOptions__ConnectionString"
        secret_name = "jobs-connection"
      }

      env {
        name        = "CachingOptions__Redis"
        secret_name = "redis-connection"
      }

      env {
        name        = "JwtOptions__SigningKey"
        secret_name = "jwt-signing-key"
      }
    }
  }
}

# The migrator is a JOB, not an app: it runs to completion and exits. It holds a Postgres advisory
# lock for the whole run, so a retried execution or an overlapping release queues rather than races.
resource "azurerm_container_app_job" "migrator" {
  name                         = "caj-migrator-${var.environment}"
  resource_group_name          = azurerm_resource_group.this.name
  location                     = azurerm_resource_group.this.location
  container_app_environment_id = azurerm_container_app_environment.this.id

  replica_timeout_in_seconds = 1800
  replica_retry_limit        = 1
  tags                       = local.tags

  manual_trigger_config {
    parallelism              = 1
    replica_completion_count = 1
  }

  secret {
    name  = "database-connection"
    value = local.connection_string["dental"]
  }

  template {
    container {
      name    = "migrator"
      image   = var.migrator_image
      cpu     = 0.5
      memory  = "1Gi"
      args    = ["apply", "--seed"]

      env {
        name        = "DatabaseOptions__ConnectionString"
        secret_name = "database-connection"
      }
    }
  }
}
