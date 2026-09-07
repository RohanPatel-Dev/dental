output "api_url" {
  description = "Public URL of the API host."
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}

output "postgres_fqdn" {
  description = "Postgres server hostname. The password lives in the state and in Key Vault, never here."
  value       = azurerm_postgresql_flexible_server.this.fqdn
}

output "migrator_job" {
  description = "Job to start before rolling the API onto a new image."
  value       = azurerm_container_app_job.migrator.name
}
