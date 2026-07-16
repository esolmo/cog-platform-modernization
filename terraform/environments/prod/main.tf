terraform {
  required_version = ">= 1.6"
  required_providers {
    aws = { source = "hashicorp/aws", version = "~> 5.0" }
  }

  backend "s3" {
    bucket         = "cog-terraform-state"
    key            = "prod/terraform.tfstate"
    region         = "us-east-1"
    encrypt        = true
    dynamodb_table = "cog-terraform-locks"
  }
}

provider "aws" {
  region = var.aws_region
  default_tags {
    tags = {
      Project     = "cog"
      Environment = "prod"
      ManagedBy   = "terraform"
    }
  }
}

module "networking" {
  source      = "../../modules/networking"
  project     = "cog"
  environment = "prod"
  vpc_cidr    = "10.1.0.0/16"
}

module "eks" {
  source              = "../../modules/eks"
  project             = "cog"
  environment         = "prod"
  vpc_id              = module.networking.vpc_id
  private_subnet_ids  = module.networking.private_subnet_ids
  node_min_size       = 3
  node_max_size       = 20
  node_desired_size   = 5
  node_instance_types = ["t3.xlarge"]
}

module "rds" {
  source              = "../../modules/rds"
  project             = "cog"
  environment         = "prod"
  vpc_id              = module.networking.vpc_id
  private_subnet_ids  = module.networking.private_subnet_ids
  db_password         = var.db_password
  instance_class      = "db.r6g.xlarge"
  allocated_storage   = 500
  allowed_cidr_blocks = ["10.1.0.0/8"]
}

module "redis" {
  source              = "../../modules/redis"
  project             = "cog"
  environment         = "prod"
  vpc_id              = module.networking.vpc_id
  private_subnet_ids  = module.networking.private_subnet_ids
  node_type           = "cache.r6g.large"
  num_cache_nodes     = 2
  allowed_cidr_blocks = ["10.1.0.0/8"]
}

module "alb" {
  source             = "../../modules/alb"
  project            = "cog"
  environment        = "prod"
  vpc_id             = module.networking.vpc_id
  public_subnet_ids  = module.networking.public_subnet_ids
}

module "betting_ui"  { source = "../../modules/s3-frontend", project = "cog", environment = "prod", app_name = "betting" }
module "admin_ui"    { source = "../../modules/s3-frontend", project = "cog", environment = "prod", app_name = "admin" }
module "reports_ui"  { source = "../../modules/s3-frontend", project = "cog", environment = "prod", app_name = "reports" }
module "lottery_ui"  { source = "../../modules/s3-frontend", project = "cog", environment = "prod", app_name = "lottery" }
module "accounts_ui" { source = "../../modules/s3-frontend", project = "cog", environment = "prod", app_name = "accounts" }

variable "aws_region"  { type = string, default = "us-east-1" }
variable "db_password" { type = string, sensitive = true }
