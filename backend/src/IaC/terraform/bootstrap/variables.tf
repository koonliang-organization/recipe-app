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

variable "bucket_name" {
  description = "Optional explicit S3 bucket name (must be globally unique). If null, a name will be generated."
  type        = string
  default     = null
}

variable "dynamodb_table_name" {
  description = "DynamoDB table name for state locks"
  type        = string
  default     = null
}

variable "state_prefix" {
  description = "Prefix subfolder under the environment path to store tfstate files"
  type        = string
  default     = "tfstate"
}
