terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

resource "random_id" "suffix" {
  byte_length = 2
}

locals {
  bucket_name = coalesce(var.bucket_name, "${var.environment}-tf-state-${random_id.suffix.hex}")
  ddb_name    = coalesce(var.dynamodb_table_name, "${var.environment}-tf-state-locks")
}

resource "aws_s3_bucket" "state" {
  bucket = local.bucket_name

  tags = {
    Environment = var.environment
    Project     = "recipe-app"
    Component   = "tf-state"
  }
}

resource "aws_s3_bucket_versioning" "state" {
  bucket = aws_s3_bucket.state.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "state" {
  bucket = aws_s3_bucket.state.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "state" {
  bucket                  = aws_s3_bucket.state.id
  block_public_acls       = true
  ignore_public_acls      = true
  block_public_policy     = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_ownership_controls" "state" {
  bucket = aws_s3_bucket.state.id
  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_policy" "state_secure" {
  bucket = aws_s3_bucket.state.id
  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Sid       = "DenyInsecureTransport",
        Effect    = "Deny",
        Principal = "*",
        Action    = "s3:*",
        Resource  = [aws_s3_bucket.state.arn, "${aws_s3_bucket.state.arn}/*"],
        Condition = { Bool = { "aws:SecureTransport" = false } }
      },
      {
        Sid       = "RequireSSE",
        Effect    = "Deny",
        Principal = "*",
        Action    = ["s3:PutObject"],
        Resource  = "${aws_s3_bucket.state.arn}/*",
        Condition = { StringNotEquals = { "s3:x-amz-server-side-encryption" = "AES256" } }
      }
    ]
  })
}

resource "aws_dynamodb_table" "locks" {
  name         = local.ddb_name
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "LockID"

  attribute {
    name = "LockID"
    type = "S"
  }

  tags = {
    Environment = var.environment
    Project     = "recipe-app"
    Component   = "tf-locks"
  }
}
