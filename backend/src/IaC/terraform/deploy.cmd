@echo off
setlocal EnableDelayedExpansion

rem Recipe App Terraform Deployment (Windows CMD)
rem Mirrors logic from deploy.sh for Windows environments

set SCRIPT_DIR=%~dp0
pushd "%SCRIPT_DIR%" >nul

set SKIP_CONFIRMATION=false

rem Parse CLI arguments first
call :parse_args %*

rem -----------------------------
rem Helper: print functions
rem -----------------------------
:print_status
echo [INFO] %~1
goto :eof

:print_warn
echo [WARN] %~1
goto :eof

:print_error
echo [ERROR] %~1
goto :eof

:print_header
echo.
echo ===============================
echo %~1
echo ===============================
goto :eof

rem -----------------------------
rem Helper: load .env file
rem -----------------------------
:load_env_file
if exist .env (
    call :print_status "Loading environment variables from .env file..."
    for /f "usebackq tokens=* delims=" %%L in (".env") do (
        set "line=%%L"
        if not "!line!"=="" (
            rem skip comments
            if not "!line:~0,1!"=="#" (
                call :set_env_from_line "!line!"
            )
        )
    )
)
goto :eof

rem Parse KEY=VALUE (simple parser; ignores lines without '=')
:set_env_from_line
set "kv=%~1"
for /f "tokens=1,* delims==" %%K in ("%kv%") do (
    set "k=%%~K"
    set "v=%%~L"
)
if not defined k goto :eof
if not defined v set "v="
rem strip surrounding quotes if present
if defined v (
    if "!v:~0,1!"=="\"" if "!v:~-1!"=="\"" set "v=!v:~1,-1!"
)
set "%k%=%v%"
goto :eof

rem -----------------------------
rem Helper: check required env vars
rem -----------------------------
:check_env_vars
call :print_status "Checking required environment variables..."
if not defined JWT_SECRET_KEY (
    call :print_error "Missing required environment variables:"
    echo   - JWT_SECRET_KEY
    echo.
    echo Please set these variables before running the deployment:
    echo   set JWT_SECRET_KEY=your-jwt-secret
    echo   set DATABASE_CONNECTION_STRING=your-db-connection
    echo.
    echo Or create a .env file with these variables and run this script again.
    exit /b 1
)
call :print_status "All required environment variables are set"
goto :eof

rem -----------------------------
rem Helper: set defaults for optional vars
rem -----------------------------
:set_defaults
if not defined JWT_ISSUER set "JWT_ISSUER=recipe-app"
if not defined JWT_AUDIENCE set "JWT_AUDIENCE=recipe-app-users"
if not defined ENVIRONMENT set "ENVIRONMENT=dev"
if not defined AWS_REGION set "AWS_REGION=ap-southeast-1"
if not defined DATABASE_CONNECTION_STRING set "DATABASE_CONNECTION_STRING="
goto :eof

rem -----------------------------
rem Helper: ensure prerequisites
rem -----------------------------
:check_tools
where terraform >nul 2>nul
if errorlevel 1 (
    call :print_error "Terraform is not installed or not in PATH."
    echo Install Terraform and ensure 'terraform' is available in PATH.
    exit /b 1
)
where dotnet >nul 2>nul
if errorlevel 1 (
    call :print_error ".NET SDK is not installed or not in PATH."
    echo Install .NET SDK 8.0+ and ensure 'dotnet' is available in PATH.
    exit /b 1
)
where powershell >nul 2>nul
if errorlevel 1 (
    call :print_error "PowerShell is required to create zip packages."
    exit /b 1
)
goto :eof

rem -----------------------------
rem Build Lambda functions
rem -----------------------------
:build_lambdas
call :print_header "Building Lambda functions..."
if not exist "%SCRIPT_DIR%build-lambdas.cmd" (
    call :print_error "build-lambdas.cmd not found in current directory"
    exit /b 1
)
call "%SCRIPT_DIR%build-lambdas.cmd"
if errorlevel 1 (
    call :print_error "Lambda build failed"
    exit /b 1
)
goto :eof

rem -----------------------------
rem Terraform steps
rem -----------------------------
:terraform_init
call :print_header "Initializing Terraform..."
terraform init
if errorlevel 1 exit /b 1
goto :eof

:terraform_plan
call :print_header "Planning Terraform deployment..."
terraform plan ^
  -var="jwt_secret_key=%JWT_SECRET_KEY%" ^
  -var="jwt_issuer=%JWT_ISSUER%" ^
  -var="jwt_audience=%JWT_AUDIENCE%" ^
  -var="database_connection_string=%DATABASE_CONNECTION_STRING%" ^
  -var="environment=%ENVIRONMENT%" ^
  -var="aws_region=%AWS_REGION%"
if errorlevel 1 exit /b 1
goto :eof

:terraform_apply
call :print_header "Applying Terraform deployment..."
terraform apply -auto-approve ^
  -var="jwt_secret_key=%JWT_SECRET_KEY%" ^
  -var="jwt_issuer=%JWT_ISSUER%" ^
  -var="jwt_audience=%JWT_AUDIENCE%" ^
  -var="database_connection_string=%DATABASE_CONNECTION_STRING%" ^
  -var="environment=%ENVIRONMENT%" ^
  -var="aws_region=%AWS_REGION%"
if errorlevel 1 exit /b 1
goto :eof

rem -----------------------------
rem Show results
rem -----------------------------
:show_results
call :print_header "Deployment Results"
call :print_status "API Gateway URL:"
terraform output -raw api_gateway_url
echo.
call :print_status "Lambda Function ARNs:"
echo   User Lambda: 
terraform output -raw user_lambda_arn
echo   Recipe Lambda: 
terraform output -raw recipe_lambda_arn
echo   Authorizer Lambda: 
terraform output -raw authorizer_lambda_arn
echo.
call :print_status "You can now test your API at:"
terraform output -raw api_gateway_url
goto :eof

rem -----------------------------
rem Help text
rem -----------------------------
:show_help
echo Recipe App Terraform Deployment (Windows)
echo.
echo Usage: %~nx0 ^[-h^|--help^] ^[-y^|--yes^]
echo.
echo Options:
echo   -h, --help     Show this help message
echo   -y, --yes      Skip confirmation prompt
echo.
echo Environment Variables (required):
echo   JWT_SECRET_KEY              JWT secret key for token signing
echo   DATABASE_CONNECTION_STRING  Database connection string (can be empty)
echo.
echo Environment Variables (optional):
echo   ENVIRONMENT                 Environment name ^(default: dev^)
echo   AWS_REGION                  AWS region ^(default: ap-southeast-1^)
echo   JWT_ISSUER                  JWT issuer ^(default: recipe-app^)
echo   JWT_AUDIENCE                JWT audience ^(default: recipe-app-users^)
echo   AWS_PROFILE                 AWS named profile to use ^(optional^)
echo.
echo Examples:
echo   rem Interactive deployment
echo   %~nx0
echo.
echo   rem Non-interactive deployment
echo   %~nx0 --yes
echo.
echo   rem With environment variables
echo   set JWT_SECRET_KEY=your-secret
echo   set DATABASE_CONNECTION_STRING=your-db-connection
echo   %~nx0
goto :eof

rem -----------------------------
rem Parse arguments
rem -----------------------------
:parse_args
if "%~1"=="" goto :after_args
if /i "%~1"=="-h"  ( call :show_help & exit /b 0 )
if /i "%~1"=="--help" ( call :show_help & exit /b 0 )
if /i "%~1"=="-y"  ( set "SKIP_CONFIRMATION=true" & shift & goto :parse_args )
if /i "%~1"=="--yes" ( set "SKIP_CONFIRMATION=true" & shift & goto :parse_args )
call :print_error "Unknown option: %~1"
echo.
call :show_help
exit /b 1

:after_args

rem -----------------------------
rem Main flow
rem -----------------------------
call :print_header "Recipe App Terraform Deployment"

call :load_env_file
call :check_env_vars
call :set_defaults
call :check_tools
call :build_lambdas
call :terraform_init
call :terraform_plan

if /i "%SKIP_CONFIRMATION%"=="true" (
    call :terraform_apply
    if errorlevel 1 exit /b 1
    call :show_results
    goto :end
)

call :print_warn "Ready to apply the deployment plan."
set /p "CONFIRM=Do you want to continue? (y/N): "
echo.
if /i "%CONFIRM%"=="y" (
    call :terraform_apply
    if errorlevel 1 exit /b 1
    call :show_results
) else (
    call :print_status "Deployment cancelled by user."
)

:end
popd >nul
endlocal
exit /b 0
