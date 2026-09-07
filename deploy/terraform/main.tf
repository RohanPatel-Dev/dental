# Azure Container Apps: the target Aspire models locally, so the topology here is the same one
# AppHost.cs describes - one API, one extracted notifications host, a migrator that runs to
# completion, Postgres, Redis and blob storage.
#
# What this deliberately does NOT do: build or push images (CI does), run migrations on a schedule
# (the job is triggered by the release), or manage DNS and certificates (they outlive the stack).

locals {
  name = "dental-${var.environment}"

  tags = merge(var.tags, {
    application = "dental"
    environment = var.environment
    managed_by  = "terraform"
  })
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.name}"
  location = var.location
  tags     = local.tags
}

# ---------------------------------------------------------------------------------------------
# Secrets
# ---------------------------------------------------------------------------------------------

resource "random_password" "postgres" {
  length  = 32
  special = false
}

resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false
}

resource "azurerm_key_vault" "this" {
  name                       = substr(replace("kv-${local.name}", "-", ""), 0, 24)
  resource_group_name        = azurerm_resource_group.this.name
  location                   = azurerm_resource_group.this.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  purge_protection_enabled   = true
  soft_delete_retention_days = 90
  rbac_authorization_enabled = true
  tags                       = local.tags
}

data "azurerm_client_config" "current" {}

# ---------------------------------------------------------------------------------------------
# Data
# ---------------------------------------------------------------------------------------------

resource "azurerm_postgresql_flexible_server" "this" {
  name                          = "psql-${local.name}"
  resource_group_name           = azurerm_resource_group.this.name
  location                      = azurerm_resource_group.this.location
  version                       = "17"
  sku_name                      = var.postgres_sku
  storage_mb                    = 32768
  administrator_login           = "dental"
  administrator_password        = random_password.postgres.result
  backup_retention_days         = 14
  geo_redundant_backup_enabled  = var.environment == "prod"
  public_network_access_enabled = false
  zone                          = "1"
  tags                          = local.tags

  lifecycle {
    # Dropping the server takes every practice's records with it.
    prevent_destroy = true
  }
}

# One logical database per host. Hangfire owns its schema outright, so two hosts sharing one would
# fight over the same queues.
resource "azurerm_postgresql_flexible_server_database" "this" {
  for_each = toset([
    "dental",
    "dental_hangfire_api",
    "dental_notifications",
    "dental_hangfire_notifications",
  ])

  name      = each.key
  server_id = azurerm_postgresql_flexible_server.this.id
  charset   = "UTF8"
  collation = "en_US.utf8"

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_redis_cache" "this" {
  name                 = "redis-${local.name}"
  resource_group_name  = azurerm_resource_group.this.name
  location             = azurerm_resource_group.this.location
  capacity             = 1
  family               = "C"
  sku_name             = "Standard"
  minimum_tls_version  = "1.2"
  non_ssl_port_enabled = false
  tags                 = local.tags
}

# Object storage is NOT provisioned here.
#
# Dental.Framework.Storage implements two providers: "local" (a folder, single machine only) and
# "s3" (any S3-compatible endpoint). Azure Blob speaks neither, so an Azure Storage Account would be
# a resource nothing could write to. Point var.storage_* at an S3-compatible service, or add an
# Azure provider to Dental.Framework.Storage and change this file with it.
