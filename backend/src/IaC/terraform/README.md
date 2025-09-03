# Terraform State: Local vs Remote

This folder supports both local state (default) and optional remote S3 state.

## Local State (default)
- No backend file is required.
- Commands:
  - `terraform init -reconfigure`
  - `terraform plan`
  - `terraform apply`
- If you previously tried S3, clean once: remove `.terraform/` and re‑init.

## Remote State (S3)
- Create a `backend.hcl` file in this folder with:
  - `bucket = "<bucket-name>"`
  - `key    = "recipe-app/<env>/tfstate/terraform.tfstate"`
  - `region = "<aws-region>"`
  - `dynamodb_table = "<locks-table>"`
  - `encrypt = true`
- Enable and init:
  - `cp backend.remote.tf.example backend.remote.tf` (optional; scripts auto-create)
  - `terraform init -reconfigure -backend-config=backend.hcl`
  - `terraform plan && terraform apply`

## Scripts
- Linux/macOS: `./deploy.sh`
- Windows: `deploy.cmd`
- Behavior:
  - If `backend.hcl` exists → initializes S3 backend (creates `backend.remote.tf` if missing).
  - Otherwise → uses local state.

## CI (GitHub Actions)
- `.github/workflows/infra.yml` writes `backend.hcl` and `backend.remote.tf` before `terraform init` to use remote S3 in CI.

## Switching
- Remote → Local: delete `backend.hcl` and `backend.remote.tf`, remove `.terraform/`, then `terraform init -reconfigure`.
- Files `backend.hcl` and `backend.remote.tf` are git-ignored; keep secrets out of VCS.

