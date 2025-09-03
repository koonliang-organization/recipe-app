terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  # Backend is intentionally not pinned here.
  # Default is local state. For remote S3 state, create a file
  # named `backend.remote.tf` with the following content and run
  # `terraform init -backend-config=backend.hcl -reconfigure`:
  #
  # terraform {
  #   backend "s3" {}
  # }
}

provider "aws" {
  region = var.aws_region
}
