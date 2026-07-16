terraform {
  required_version = ">= 1.6"
  required_providers {
    aws = { source = "hashicorp/aws", version = "~> 5.0" }
  }

  backend "s3" {
    bucket         = "cog-terraform-state"
    key            = "dev/terraform.tfstate"
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
      Environment = "dev"
      ManagedBy   = "terraform"
    }
  }
}

module "networking" {
  source      = "../../modules/networking"
  project     = "cog"
  environment = "dev"
  vpc_cidr    = "10.0.0.0/16"
}

module "eks" {
  source              = "../../modules/eks"
  project             = "cog"
  environment         = "dev"
  vpc_id              = module.networking.vpc_id
  private_subnet_ids  = module.networking.private_subnet_ids
  node_min_size       = 2
  node_max_size       = 5
  node_desired_size   = 2
  node_instance_types = ["t3.large"]
}

module "rds" {
  source              = "../../modules/rds"
  project             = "cog"
  environment         = "dev"
  vpc_id              = module.networking.vpc_id
  private_subnet_ids  = module.networking.private_subnet_ids
  db_password         = var.db_password
  instance_class      = "db.t3.large"
  allowed_cidr_blocks = ["10.0.0.0/8"]
}

module "redis" {
  source              = "../../modules/redis"
  project             = "cog"
  environment         = "dev"
  vpc_id              = module.networking.vpc_id
  private_subnet_ids  = module.networking.private_subnet_ids
  allowed_cidr_blocks = ["10.0.0.0/8"]
}

module "alb" {
  source             = "../../modules/alb"
  project            = "cog"
  environment        = "dev"
  vpc_id             = module.networking.vpc_id
  public_subnet_ids  = module.networking.public_subnet_ids
}

module "betting_ui" {
  source      = "../../modules/s3-frontend"
  project     = "cog"
  environment = "dev"
  app_name    = "betting"
}

module "admin_ui" {
  source      = "../../modules/s3-frontend"
  project     = "cog"
  environment = "dev"
  app_name    = "admin"
}

module "accounts_ui" {
  source      = "../../modules/s3-frontend"
  project     = "cog"
  environment = "dev"
  app_name    = "accounts"
}

module "lottery_ui" {
  source      = "../../modules/s3-frontend"
  project     = "cog"
  environment = "dev"
  app_name    = "lottery"
}

module "reports_ui" {
  source      = "../../modules/s3-frontend"
  project     = "cog"
  environment = "dev"
  app_name    = "reports"
}

variable "aws_region"  { type = string, default = "us-east-1" }
variable "db_password" { type = string, sensitive = true }

output "cluster_endpoint"     { value = module.eks.cluster_endpoint }
output "rds_endpoint"         { value = module.rds.endpoint }
output "redis_endpoint"       { value = module.redis.primary_endpoint }
output "alb_dns_name"         { value = module.alb.alb_dns_name }
output "betting_ui_domain"    { value = module.betting_ui.cloudfront_domain }
output "admin_ui_domain"      { value = module.admin_ui.cloudfront_domain }
output "accounts_ui_domain"   { value = module.accounts_ui.cloudfront_domain }
output "lottery_ui_domain"    { value = module.lottery_ui.cloudfront_domain }
output "reports_ui_domain"    { value = module.reports_ui.cloudfront_domain }
