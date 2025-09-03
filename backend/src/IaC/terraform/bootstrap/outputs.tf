output "backend_bucket" {
  value       = aws_s3_bucket.state.bucket
  description = "S3 bucket for Terraform state"
}

output "backend_table" {
  value       = aws_dynamodb_table.locks.name
  description = "DynamoDB table for Terraform state locks"
}

output "backend_prefix" {
  value       = "${var.environment}/${var.state_prefix}"
  description = "Recommended prefix path to store state under the environment"
}

output "backend_key_example" {
  value       = "recipe-app/${var.environment}/${var.state_prefix}/terraform.tfstate"
  description = "Example key to use in backend.hcl"
}

