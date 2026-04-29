output "vpc_id" {
  description = "VPC ID"
  value       = aws_vpc.main.id
}

output "eks_cluster_name" {
  description = "EKS cluster name"
  value       = aws_eks_cluster.cluster.name
}

output "eks_cluster_endpoint" {
  description = "EKS endpoint"
  value       = aws_eks_cluster.cluster.endpoint
}

output "eks_node_role_arn" {
  description = "ARN of the node IAM role"
  value       = aws_iam_role.eks_node_role.arn
}
