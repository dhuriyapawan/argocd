# Use an existing VPC

data "aws_vpc" "main" {
  id = "vpc-036750db266014be4"
}

# Internet gateway attached to the existing VPC
resource "aws_internet_gateway" "igw" {
  vpc_id = data.aws_vpc.main.id
  tags = {
    Name = "eks-igw"
  }
}

# Public subnets (created in the existing VPC)
resource "aws_subnet" "public" {
  count                   = length(var.azs)
  vpc_id                  = data.aws_vpc.main.id
  cidr_block              = element(var.public_subnet_cidrs, count.index)
  availability_zone       = element(var.azs, count.index)
  map_public_ip_on_launch = true
  tags = {
    Name = "eks-public-${count.index}"
  }
}

# Private subnets (created in the existing VPC)
resource "aws_subnet" "private" {
  count             = length(var.azs)
  vpc_id            = data.aws_vpc.main.id
  cidr_block        = element(var.private_subnet_cidrs, count.index)
  availability_zone = element(var.azs, count.index)
  tags = {
    Name = "eks-private-${count.index}"
  }
}

# Public route table + association
resource "aws_route_table" "public" {
  vpc_id = data.aws_vpc.main.id
  tags = {
    Name = "eks-public-rt"
  }
}

resource "aws_route" "default" {
  route_table_id         = aws_route_table.public.id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = aws_internet_gateway.igw.id
}

resource "aws_route_table_association" "public" {
  count          = length(aws_subnet.public)
  subnet_id      = element(aws_subnet.public[*].id, count.index)
  route_table_id = aws_route_table.public.id
}
