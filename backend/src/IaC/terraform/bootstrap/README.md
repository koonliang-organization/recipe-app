Bootstrap Terraform Backend (S3 + DynamoDB)

This module provisions a secure S3 bucket and DynamoDB table to use as a Terraform remote state backend. It also standardizes the state key to live under a subfolder named `tfstate` within your environment path.

What it creates
- Private S3 bucket with:
  - Versioning enabled
  - Default encryption (SSE-S3 AES256)
  - Public access fully blocked
  - Bucket owner enforced (no ACLs)
- DynamoDB table for Terraform state locking

Inputs
- `environment`: Environment label (e.g., dev, staging, prod)
- `aws_region`: AWS region (default ap-southeast-1)
- `bucket_name`: Optional. If omitted, a unique name is generated.
- `dynamodb_table_name`: Optional. Defaults to `<environment>-tf-state-locks`.
- `state_prefix`: Path segment to place state files under. Default `tfstate`.

Outputs
- `backend_bucket`: Name of the S3 bucket
- `backend_table`: Name of the DynamoDB table
- `backend_prefix`: Recommended prefix for the state key, e.g. `dev/tfstate`
- `backend_key_example`: Example key to use: `recipe-app/dev/tfstate/terraform.tfstate`

Usage
1. Initialize and apply this module once per account/region:
   terraform init
   terraform apply -auto-approve \
     -var="environment=dev" \
     -var="aws_region=ap-southeast-1"

2. Configure your main stack to use the backend. Example backend.hcl:
   bucket = "<backend_bucket>"
   key    = "recipe-app/dev/tfstate/terraform.tfstate"
   region = "ap-southeast-1"
   dynamodb_table = "<backend_table>"
   encrypt = true

Notes
- Terraform cannot provision and then immediately use its own backend in the same state. This bootstrap should be a separate state or applied once with local state.
