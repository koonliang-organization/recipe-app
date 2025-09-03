terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # S3 backend. Values are placeholders and are overridden via -backend-config.
  backend "s3" {
    bucket = "placeholder"
    key    = "placeholder"
    region = "ap-southeast-1"
    encrypt = true
    # dynamodb_table intentionally omitted; provided via backend.hcl
  }
}

provider "aws" {
  region = var.aws_region
}
