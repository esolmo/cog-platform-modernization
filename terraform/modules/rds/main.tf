terraform {
  required_providers {
    aws = { source = "hashicorp/aws", version = "~> 5.0" }
  }
}

variable "project"            { type = string }
variable "environment"        { type = string }
variable "vpc_id"             { type = string }
variable "private_subnet_ids" { type = list(string) }
variable "db_username"        { type = string, default = "cogadmin" }
variable "db_password"        { type = string, sensitive = true }
variable "instance_class"     { type = string, default = "db.t3.large" }
variable "allocated_storage"  { type = number, default = 100 }
variable "allowed_cidr_blocks" { type = list(string) }

locals { name = "${var.project}-${var.environment}" }

resource "aws_db_subnet_group" "main" {
  name       = "${local.name}-db-subnet-group"
  subnet_ids = var.private_subnet_ids
  tags       = { Name = "${local.name}-db-subnet-group" }
}

resource "aws_security_group" "rds" {
  name        = "${local.name}-rds-sg"
  description = "Allow SQL Server from EKS nodes"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 1433
    to_port     = 1433
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-rds-sg" }
}

resource "aws_db_instance" "main" {
  identifier             = "${local.name}-sqlserver"
  engine                 = "sqlserver-se"
  engine_version         = "15.00.4316.3.v1"
  instance_class         = var.instance_class
  allocated_storage      = var.allocated_storage
  storage_type           = "gp3"
  storage_encrypted      = true
  username               = var.db_username
  password               = var.db_password
  license_model          = "license-included"
  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  multi_az               = var.environment == "prod"
  deletion_protection    = var.environment == "prod"
  backup_retention_period = var.environment == "prod" ? 7 : 1
  skip_final_snapshot    = var.environment != "prod"
  publicly_accessible    = false

  tags = { Name = "${local.name}-rds" }
}

output "endpoint" { value = aws_db_instance.main.endpoint }
output "port"     { value = aws_db_instance.main.port }
