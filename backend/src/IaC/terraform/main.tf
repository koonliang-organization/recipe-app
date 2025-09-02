terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Use S3 backend configured via -backend-config in CI or local runs
  backend "s3" {}
}

provider "aws" {
  region = var.aws_region
}
