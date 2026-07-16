# COG Platform — Terraform Infrastructure

Deploys all COG microservices infrastructure to AWS using Terraform 1.6+.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      AWS Account                        │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │              VPC  (10.x.0.0/16)                  │  │
│  │                                                  │  │
│  │  Public Subnets (×3 AZs)    Private Subnets      │  │
│  │  ┌─────────┐               ┌──────────────────┐  │  │
│  │  │   ALB   │               │   EKS Node Group  │  │  │
│  │  │  :80→   │               │  t3.large × 2-8   │  │  │
│  │  │  :443   │               │                  │  │  │
│  │  └────┬────┘               │  auth-svc  :5001  │  │  │
│  │       │                    │  accts-svc :5002  │  │  │
│  │  NAT  │                    │  admin-svc :5003  │  │  │
│  │  GW   │                    │  alerts-svc:5004  │  │  │
│  │       │                    │  betting-svc:5005 │  │  │
│  │       │                    │  lottery-svc:5006 │  │  │
│  │       │                    │  reports-svc:5007 │  │  │
│  │       │                    └──────────────────┘  │  │
│  │       │                    ┌──────────────────┐  │  │
│  │       │                    │  RDS SQL Server   │  │  │
│  │       │                    │  :1433            │  │  │
│  │       │                    └──────────────────┘  │  │
│  │       │                    ┌──────────────────┐  │  │
│  │       │                    │  ElastiCache Redis│  │  │
│  │       │                    │  :6379            │  │  │
│  │       │                    └──────────────────┘  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  S3 + CloudFront (one distribution per SPA):           │
│  betting-ui · admin-ui · accounts-ui · lottery-ui · reports-ui │
└─────────────────────────────────────────────────────────┘
```

## Module Reference

| Module | Description |
|---|---|
| `networking` | VPC, 3 public + 3 private subnets, IGW, NAT, route tables |
| `eks` | EKS cluster + managed node group + IAM roles + OIDC provider |
| `rds` | RDS SQL Server SE 15.x, encrypted storage, subnet group, security group |
| `redis` | ElastiCache Redis replication group, TLS, subnet group |
| `alb` | Application Load Balancer, HTTP→HTTPS redirect, security group |
| `s3-frontend` | S3 bucket (private), CloudFront OAC distribution, SPA 404 routing |

## Environments

| Environment | VPC CIDR | EKS nodes | RDS class | Notes |
|---|---|---|---|---|
| `dev` | 10.0.0.0/16 | 2–5 × t3.large | db.t3.large | Single-AZ RDS, 1 Redis node |
| `staging` | 10.2.0.0/16 | 2–8 × t3.large | db.t3.xlarge | Single-AZ RDS, 1 Redis node |
| `prod` | 10.1.0.0/16 | 3–20 × t3.xlarge | db.r6g.xlarge | Multi-AZ RDS, 2 Redis nodes |

## Deploying

### Prerequisites

```bash
# Install Terraform 1.6+
terraform --version

# Configure AWS credentials
aws configure   # or use IAM role / OIDC

# Create remote state bucket (one-time)
aws s3 mb s3://cog-terraform-state --region us-east-1
aws s3api put-bucket-versioning \
  --bucket cog-terraform-state \
  --versioning-configuration Status=Enabled
aws dynamodb create-table \
  --table-name cog-terraform-locks \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST \
  --region us-east-1
```

### Apply an environment

```bash
cd terraform/environments/dev

terraform init
terraform plan -var="db_password=$DB_PASSWORD"
terraform apply -var="db_password=$DB_PASSWORD"
```

### Tear down (dev only — prod has deletion protection)

```bash
terraform destroy -var="db_password=$DB_PASSWORD"
```

## Secrets Management

- **Database password**: passed as `var.db_password` — set via CI secret `TF_VAR_db_password`
- **JWT secret**: stored in AWS Secrets Manager `cog-{env}-jwt-secret` — injected into pods via K8s secret
- **SMTP credentials**: stored in `SystemConfigurations` table (encrypted flag)

## Adding a New Service to EKS

1. Build Docker image and push to ECR
2. Add `k8s/deployment.yaml` in the service directory
3. Update `k8s/deployment.yaml` with correct image tag via `envsubst`
4. `kubectl apply -f k8s/deployment.yaml`

The ALB Ingress Controller routes traffic by hostname/path prefix defined in each service's Ingress resource.
