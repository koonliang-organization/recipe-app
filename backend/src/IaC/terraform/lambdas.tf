# CloudWatch Log Groups (must be created before Lambda functions)
resource "aws_cloudwatch_log_group" "user_lambda" {
  name              = "/aws/lambda/${var.environment}-recipe-app-user"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "recipe_lambda" {
  name              = "/aws/lambda/${var.environment}-recipe-app-recipe"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "authorizer_lambda" {
  name              = "/aws/lambda/${var.environment}-recipe-app-authorizer"
  retention_in_days = 14
}

# User Lambda Function
resource "aws_lambda_function" "user_lambda" {
  filename         = "lambdas/user-lambda.zip"
  function_name    = "${var.environment}-recipe-app-user"
  role            = aws_iam_role.user_lambda_role.arn
  handler         = "User"
  runtime         = "dotnet8"
  memory_size     = 512
  timeout         = 30
  
  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = var.environment
      JWT_SECRET_KEY        = var.jwt_secret_key
      JWT_ISSUER           = var.jwt_issuer
      JWT_AUDIENCE         = var.jwt_audience
      CONNECTION_STRING    = var.database_connection_string
    }
  }

  depends_on = [
    aws_iam_role_policy_attachment.user_lambda_basic_execution,
    aws_cloudwatch_log_group.user_lambda,
  ]
}

# Recipe Lambda Function  
resource "aws_lambda_function" "recipe_lambda" {
  filename         = "lambdas/recipe-lambda.zip"
  function_name    = "${var.environment}-recipe-app-recipe"
  role            = aws_iam_role.recipe_lambda_role.arn
  handler         = "Recipe"
  runtime         = "dotnet8"
  memory_size     = 512
  timeout         = 30

  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = var.environment
      CONNECTION_STRING     = var.database_connection_string
    }
  }

  depends_on = [
    aws_iam_role_policy_attachment.recipe_lambda_basic_execution,
    aws_cloudwatch_log_group.recipe_lambda,
  ]
}

# Authorizer Lambda Function
resource "aws_lambda_function" "authorizer_lambda" {
  filename         = "lambdas/authorizer-lambda.zip"
  function_name    = "${var.environment}-recipe-app-authorizer"
  role            = aws_iam_role.authorizer_lambda_role.arn
  handler         = "Authorizer::Authorizer.Function::FunctionHandler"
  runtime         = "dotnet8"
  memory_size     = 256
  timeout         = 10

  environment {
    variables = {
      JWT_SECRET_KEY = var.jwt_secret_key
      JWT_ISSUER    = var.jwt_issuer
      JWT_AUDIENCE  = var.jwt_audience
    }
  }

  depends_on = [
    aws_iam_role_policy_attachment.authorizer_lambda_basic_execution,
    aws_cloudwatch_log_group.authorizer_lambda,
  ]
}

# Lambda Permissions for API Gateway
resource "aws_lambda_permission" "allow_api_gateway_user" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.user_lambda.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_api_gateway_rest_api.recipe_api.execution_arn}/*/*"
}

resource "aws_lambda_permission" "allow_api_gateway_recipe" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.recipe_lambda.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_api_gateway_rest_api.recipe_api.execution_arn}/*/*"
}

resource "aws_lambda_permission" "allow_api_gateway_authorizer" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.authorizer_lambda.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_api_gateway_rest_api.recipe_api.execution_arn}/authorizers/*"
}