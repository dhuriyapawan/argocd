resource "aws_eks_cluster" "cluster" {
    name     = "helm-argo-eks"
    role_arn = aws_iam_role.eks_node_role.arn
    version  = "1.30"

    vpc_config {
      subnet_ids = concat(
        aws_subnet.private[*].id,
        aws_subnet.public[*].id
      )
      endpoint_private_access = true
      endpoint_public_access  = true
    }

    tags = {
      Environment = "dev"
    }
  }

  # Managed node group using the same admin role
  resource "aws_eks_node_group" "ng" {
    cluster_name    = aws_eks_cluster.cluster.name
    node_group_name = "default-ng"
    node_role       = aws_iam_role.eks_node_role.arn
    subnet_ids      = aws_subnet.private[*].id
    scaling_config {
      desired_size = 2
      max_size     = 3
      min_size     = 1
    }

    # Use latest Amazon Linux 2 EKS‑optimized AMI
    ami_type       = "AL2_x86_64"
    instance_types = ["t3.medium"]
    tags = {
      Environment = "dev"
    }
  }