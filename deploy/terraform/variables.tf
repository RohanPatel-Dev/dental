variable "environment" {
  description = "Environment name, used in every resource name (dev, staging, prod)."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9]{2,10}$", var.environment))
    error_message = "The environment must be 2-10 lower-case alphanumeric characters."
  }
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "uksouth"
}

variable "api_image" {
  description = "Fully qualified image for the API host."
  type        = string
}

variable "migrator_image" {
  description = "Fully qualified image for the migrator job."
  type        = string
}

variable "notifications_image" {
  description = "Fully qualified image for the extracted notifications host."
  type        = string
}

variable "postgres_sku" {
  description = "SKU for the Postgres flexible server."
  type        = string
  default     = "B_Standard_B2s"
}

variable "api_min_replicas" {
  description = "Minimum API replicas. Keep at least 1 outside development: scaling to zero makes the first request of the day pay for a cold start and a fresh connection pool."
  type        = number
  default     = 1
}

variable "api_max_replicas" {
  description = "Maximum API replicas."
  type        = number
  default     = 5
}

variable "tags" {
  description = "Tags applied to every resource."
  type        = map(string)
  default     = {}
}

variable "storage_service_url" {
  description = "S3-compatible endpoint for patient documents. Leave empty in an environment that does not store documents yet; the API then falls back to local storage, which is per-replica and therefore single-machine only."
  type        = string
  default     = ""
}

variable "storage_bucket" {
  description = "Bucket documents are written to."
  type        = string
  default     = "dental"
}

variable "storage_access_key" {
  description = "Access key for the S3-compatible endpoint."
  type        = string
  default     = ""
  sensitive   = true
}

variable "storage_secret_key" {
  description = "Secret key for the S3-compatible endpoint."
  type        = string
  default     = ""
  sensitive   = true
}
