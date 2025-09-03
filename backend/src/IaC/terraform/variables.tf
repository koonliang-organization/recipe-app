variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "ap-southeast-1"
}

variable "jwt_secret_key" {
  description = "JWT secret key"
  type        = string
  default     = "JHdNc+FmHGMvUYRri9NhWLAlUoLCZX5OXTgACXeRS84="
  sensitive   = true
}

variable "jwt_issuer" {
  description = "JWT issuer"
  type        = string
  default     = "recipe-app"
}

variable "jwt_audience" {
  description = "JWT audience"
  type        = string
  default     = "recipe-app-users"
}

variable "database_connection_string" {
  description = "Database connection string (can be empty if not using database)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "enable_seed_data" {
  description = "Enable database seeding on application startup"
  type        = bool
  default     = true
}

variable "seed_only_if_empty" {
  description = "Only seed data if database is empty"
  type        = bool
  default     = false
}

# -----------------------------
# MySQL RDS (optional)
# -----------------------------

variable "enable_rds" {
  description = "Enable provisioning of MySQL RDS instance"
  type        = bool
  default     = false
}

variable "mysql_db_name" {
  description = "Initial database name for MySQL"
  type        = string
  default     = "recipeapp"
}

variable "mysql_username" {
  description = "Master username for MySQL"
  type        = string
  default     = "appuser"
}

variable "mysql_password" {
  description = "Master password for MySQL (min 8 chars)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "mysql_allowed_cidrs" {
  description = "CIDR blocks allowed to access MySQL (use restrictive ranges in production)"
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "mysql_publicly_accessible" {
  description = "Whether the RDS instance is publicly accessible (true for easy dev; false for private)"
  type        = bool
  default     = false
}

variable "mysql_allocated_storage" {
  description = "Allocated storage in GB (20 to stay in free tier)"
  type        = number
  default     = 20
}

variable "mysql_backup_retention_days" {
  description = "Backup retention in days (free tier includes backups up to allocated storage)"
  type        = number
  default     = 7
}
