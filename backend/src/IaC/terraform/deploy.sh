#!/bin/bash

# Deployment script for Recipe App Terraform infrastructure
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}$1${NC}"
}

# Check if required environment variables are set
check_env_vars() {
    print_status "Checking required environment variables..."
    
    local required_vars=("JWT_SECRET_KEY")
    local missing_vars=()
    
    for var in "${required_vars[@]}"; do
        if [[ -z "${!var}" ]]; then
            missing_vars+=("$var")
        fi
    done
    
    if [[ ${#missing_vars[@]} -gt 0 ]]; then
        print_error "Missing required environment variables:"
        for var in "${missing_vars[@]}"; do
            echo "  - $var"
        done
        echo ""
        echo "Please set these variables before running the deployment:"
        echo "  export JWT_SECRET_KEY='your-jwt-secret'"
        echo "  export DATABASE_CONNECTION_STRING='your-db-connection'"
        echo ""
        echo "Or create a .env file with these variables and run:"
        echo "  source .env"
        exit 1
    fi
    
    print_status "✅ All required environment variables are set"
}

# Load environment variables from .env file if it exists
load_env_file() {
    if [[ -f .env ]]; then
        print_status "Loading environment variables from .env file..."
        export $(grep -v '^#' .env | xargs)
    fi
}

# Build Lambda functions
build_lambdas() {
    print_header "🔨 Building Lambda functions..."
    
    if [[ ! -f "./build-lambdas.sh" ]]; then
        print_error "build-lambdas.sh not found in current directory"
        exit 1
    fi
    
    ./build-lambdas.sh
}

# Initialize Terraform
init_terraform() {
    print_header "🚀 Initializing Terraform..."

    if [[ -f backend.hcl ]]; then
        # Ensure an S3 backend block exists when using backend.hcl
        if [[ ! -f backend.remote.tf ]]; then
            print_status "Creating temporary backend.remote.tf for S3 backend"
            cat > backend.remote.tf <<'EOF'
terraform {
  backend "s3" {}
}
EOF
        fi
        terraform init -reconfigure -backend-config=backend.hcl
    else
        # Default to local backend
        terraform init -reconfigure
    fi
}

# Plan Terraform deployment
plan_deployment() {
    print_header "📋 Planning Terraform deployment..."
    
    terraform plan \
        -var="jwt_secret_key=${JWT_SECRET_KEY}" \
        -var="jwt_issuer=${JWT_ISSUER:-recipe-app}" \
        -var="jwt_audience=${JWT_AUDIENCE:-recipe-app-users}" \
        -var="database_connection_string=${DATABASE_CONNECTION_STRING}" \
        -var="environment=${ENVIRONMENT:-dev}" \
        -var="aws_region=${AWS_REGION:-ap-southeast-1}"
}

# Apply Terraform deployment
apply_deployment() {
    print_header "🚀 Applying Terraform deployment..."
    terraform apply -auto-approve \
        -var="jwt_secret_key=${JWT_SECRET_KEY}" \
        -var="jwt_issuer=${JWT_ISSUER:-recipe-app}" \
        -var="jwt_audience=${JWT_AUDIENCE:-recipe-app-users}" \
        -var="database_connection_string=${DATABASE_CONNECTION_STRING}" \
        -var="environment=${ENVIRONMENT:-dev}" \
        -var="aws_region=${AWS_REGION:-ap-southeast-1}"
}

# Show deployment results
show_results() {
    print_header "✅ Deployment Results"
    
    echo ""
    print_status "API Gateway URL:"
    terraform output -raw api_gateway_url
    
    echo ""
    print_status "Lambda Function ARNs:"
    echo "  User Lambda: $(terraform output -raw user_lambda_arn)"
    echo "  Recipe Lambda: $(terraform output -raw recipe_lambda_arn)"
    echo "  Authorizer Lambda: $(terraform output -raw authorizer_lambda_arn)"
    
    echo ""
    print_status "🎉 Deployment completed successfully!"
    echo ""
    print_status "You can now test your API at:"
    terraform output -raw api_gateway_url
}

# Cleanup function
cleanup() {
    if [[ -f tfplan ]]; then
        print_status "Cleaning up temporary files..."
        rm -f tfplan
    fi
}

# Set trap to cleanup on exit
trap cleanup EXIT

# Main deployment flow
main() {
    print_header "🚀 Recipe App Terraform Deployment"
    echo ""
    
    # Load environment variables
    load_env_file
    
    # Check environment variables
    check_env_vars
    
    # Build Lambda functions
    build_lambdas
    
    # Initialize Terraform
    init_terraform
    
    # Plan deployment
    plan_deployment
    
    # Ask for confirmation before applying
    echo ""
    print_warning "Ready to apply the deployment plan."
    read -p "Do you want to continue? (y/N): " -n 1 -r
    echo ""
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        # Apply deployment
        apply_deployment
        
        # Show results
        show_results
    else
        print_status "Deployment cancelled by user."
        exit 0
    fi
}

# Help function
show_help() {
    echo "Recipe App Terraform Deployment Script"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help     Show this help message"
    echo "  -y, --yes      Skip confirmation prompt"
    echo ""
    echo "Environment Variables (required):"
    echo "  JWT_SECRET_KEY              JWT secret key for token signing"
    echo "  DATABASE_CONNECTION_STRING  Database connection string"
    echo ""
    echo "Environment Variables (optional):"
    echo "  ENVIRONMENT                 Environment name (default: dev)"
    echo "  AWS_REGION                  AWS region (default: ap-southeast-1)"
    echo "  JWT_ISSUER                  JWT issuer (default: recipe-app)"
    echo "  JWT_AUDIENCE                JWT audience (default: recipe-app-users)"
    echo ""
    echo "Examples:"
    echo "  # Interactive deployment"
    echo "  $0"
    echo ""
    echo "  # Non-interactive deployment"
    echo "  $0 --yes"
    echo ""
    echo "  # With environment variables"
    echo "  export JWT_SECRET_KEY='your-secret'"
    echo "  export DATABASE_CONNECTION_STRING='your-db-connection'"
    echo "  $0"
}

# Parse command line arguments
SKIP_CONFIRMATION=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -y|--yes)
            SKIP_CONFIRMATION=true
            shift
            ;;
        *)
            print_error "Unknown option: $1"
            echo ""
            show_help
            exit 1
            ;;
    esac
done

# Modify main function for non-interactive mode
if [[ "$SKIP_CONFIRMATION" == "true" ]]; then
    main_non_interactive() {
        print_header "🚀 Recipe App Terraform Deployment (Non-Interactive)"
        echo ""
        
        load_env_file
        check_env_vars
        build_lambdas
        init_terraform
        plan_deployment
        apply_deployment
        show_results
    }
    
    main_non_interactive
else
    main
fi
