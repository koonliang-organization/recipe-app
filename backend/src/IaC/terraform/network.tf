###############################################
# VPC and Private Subnets (for RDS + Lambdas)
###############################################

# Only provision networking when RDS is enabled
data "aws_availability_zones" "available" {
  state = "available"
}

resource "aws_vpc" "recipe" {
  count                = var.enable_rds ? 1 : 0
  cidr_block           = "10.0.0.0/16"
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = {
    Name        = "${var.environment}-recipe-vpc"
    Environment = var.environment
    Project     = "recipe-app"
  }
}

# Two private subnets in different AZs (required by RDS subnet group)
resource "aws_subnet" "private_a" {
  count                   = var.enable_rds ? 1 : 0
  vpc_id                  = aws_vpc.recipe[0].id
  cidr_block              = "10.0.1.0/24"
  availability_zone       = data.aws_availability_zones.available.names[0]
  map_public_ip_on_launch = false

  tags = {
    Name        = "${var.environment}-private-a"
    Environment = var.environment
    Tier        = "private"
  }
}

resource "aws_subnet" "private_b" {
  count                   = var.enable_rds ? 1 : 0
  vpc_id                  = aws_vpc.recipe[0].id
  cidr_block              = "10.0.2.0/24"
  availability_zone       = data.aws_availability_zones.available.names[1]
  map_public_ip_on_launch = false

  tags = {
    Name        = "${var.environment}-private-b"
    Environment = var.environment
    Tier        = "private"
  }
}

# Private route table (no route to Internet)
resource "aws_route_table" "private" {
  count  = var.enable_rds ? 1 : 0
  vpc_id = aws_vpc.recipe[0].id

  tags = {
    Name        = "${var.environment}-private-rt"
    Environment = var.environment
  }
}

resource "aws_route_table_association" "private_a" {
  count          = var.enable_rds ? 1 : 0
  subnet_id      = aws_subnet.private_a[0].id
  route_table_id = aws_route_table.private[0].id
}

resource "aws_route_table_association" "private_b" {
  count          = var.enable_rds ? 1 : 0
  subnet_id      = aws_subnet.private_b[0].id
  route_table_id = aws_route_table.private[0].id
}

# Security group for Lambdas (egress only)
resource "aws_security_group" "lambda_sg" {
  count       = var.enable_rds ? 1 : 0
  name        = "${var.environment}-lambda-sg"
  description = "Security group for VPC Lambdas"
  vpc_id      = aws_vpc.recipe[0].id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Environment = var.environment
    Project     = "recipe-app"
    Component   = "lambda"
  }
}

