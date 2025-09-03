###############################################
# MySQL RDS (Free Tier Friendly)
###############################################

# Note:
# - Designed to stay within AWS Free Tier where available:
#   - Single-AZ (multi_az = false)
#   - Instance class: db.t3.micro
#   - 20 GB gp2 storage
#   - Private subnets only (not publicly accessible)
# - Creation is gated by var.enable_rds to avoid breaking existing flows.

# Security group for MySQL (allow from Lambda SG only)
resource "aws_security_group" "mysql_sg" {
  count       = var.enable_rds ? 1 : 0
  name        = "${var.environment}-mysql-sg"
  description = "Security group for MySQL RDS instance"
  vpc_id      = aws_vpc.recipe[0].id

  ingress {
    description     = "MySQL from Lambdas"
    from_port       = 3306
    to_port         = 3306
    protocol        = "tcp"
    security_groups = [aws_security_group.lambda_sg[0].id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Environment = var.environment
    Project     = "recipe-app"
    Component   = "mysql"
  }
}

# DB subnet group across private subnets in our VPC (min 2 AZs required by RDS)
resource "aws_db_subnet_group" "mysql" {
  count       = var.enable_rds ? 1 : 0
  name        = "${var.environment}-mysql-subnets"
  description = "Subnet group for MySQL RDS"
  subnet_ids  = [aws_subnet.private_a[0].id, aws_subnet.private_b[0].id]

  tags = {
    Environment = var.environment
    Project     = "recipe-app"
    Component   = "mysql"
  }
}

# RDS MySQL instance (free-tier friendly)
resource "aws_db_instance" "mysql" {
  count                        = var.enable_rds ? 1 : 0
  identifier                   = "${var.environment}-recipe-mysql"
  engine                       = "mysql"
  # Let AWS pick the latest supported minor for 8.0 family if available in region
  # engine_version             = "8.0"
  instance_class               = "db.t3.micro"
  allocated_storage            = var.mysql_allocated_storage
  storage_type                 = "gp2"
  multi_az                     = false
  publicly_accessible          = var.mysql_publicly_accessible
  port                         = 3306

  db_subnet_group_name         = aws_db_subnet_group.mysql[0].name
  vpc_security_group_ids       = [aws_security_group.mysql_sg[0].id]

  db_name                      = var.mysql_db_name
  username                     = var.mysql_username
  password                     = var.mysql_password

  backup_retention_period      = var.mysql_backup_retention_days
  skip_final_snapshot          = true
  deletion_protection          = false
  auto_minor_version_upgrade   = true
  apply_immediately            = true

  tags = {
    Environment = var.environment
    Project     = "recipe-app"
    Component   = "mysql"
  }
}

