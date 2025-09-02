#!/bin/bash
set -euo pipefail

echo "Building Lambda functions..."

# Resolve script directory and paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../" && pwd)"
LAMBDA_OUT_DIR="$SCRIPT_DIR/lambdas"

# Ensure output directory exists
mkdir -p "$LAMBDA_OUT_DIR"

build_and_zip() {
  local name="$1"   # Project folder under backend/src/Lambdas
  local zipname="$2" # Output zip file name (without path)

  echo "Building $name Lambda..."
  local proj_dir="$REPO_ROOT/src/Lambdas/$name"
  local publish_dir="$proj_dir/publish"

  rm -rf "$publish_dir"
  dotnet publish "$proj_dir" -c Release -r linux-x64 --self-contained false -o "$publish_dir"

  echo "Packaging $zipname..."
  python3 - <<PYZ
import zipfile, os
base = r"$publish_dir"
out = r"$LAMBDA_OUT_DIR/$zipname"
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as zf:
    for root, _, files in os.walk(base):
        for f in files:
            p = os.path.join(root, f)
            zf.write(p, os.path.relpath(p, base))
print(out)
PYZ
}

build_and_zip "User" "user-lambda.zip"
build_and_zip "Recipe" "recipe-lambda.zip"
build_and_zip "Authorizer" "authorizer-lambda.zip"

echo "All Lambda functions built successfully!"
echo "Generated files:"
ls -la "$LAMBDA_OUT_DIR"
