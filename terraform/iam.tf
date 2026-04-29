 # IAM role for EKS node group (admin rights)
  resource "aws_iam_role" "eks_node_role" {
    name = "eks-node-admin-role"

    assume_role_policy = jsonencode({
      Version = "2012-10-17"
      Statement = [{
        Action    = "sts:AssumeRole"
        Effect    = "Allow"
        Principal = { Service = "ec2.amazonaws.com" }
      }]
    })
  }

  # Attach AdministratorAccess policy (full admin rights)
  resource "aws_iam_role_policy_attachment" "admin_attach" {
    role       = aws_iam_role.eks_node_role.name
    policy_arn = "arn:aws:iam::aws:policy/AdministratorAccess"
  }