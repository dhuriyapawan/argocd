import subprocess
import json
import sys
from pathlib import Path

# Optional: use boto3 to check AWS resources (IAM role, VPC)
try:
    import boto3
    from botocore.exceptions import ClientError
except ImportError:
    boto3 = None


def run_cmd(cmd, capture_output=True, check=False):
    """Run a shell command and return (returncode, stdout, stderr)."""
    result = subprocess.run(
        cmd,
        shell=True,
        capture_output=capture_output,
        text=True,
    )
    if check and result.returncode != 0:
        print(f"Command failed: {cmd}\n{result.stderr}")
        sys.exit(result.returncode)
    return result.returncode, result.stdout.strip(), result.stderr.strip()


def fmt_check():
    rc, out, err = run_cmd("terraform fmt -check -recursive", check=False)
    if rc != 0:
        print("Formatting issues detected, fixing them automatically.")
        run_cmd("terraform fmt -recursive", check=True)
    else:
        print("All files are properly formatted.")


def validate():
    rc, out, err = run_cmd("terraform validate", check=True)
    print(out)


def plan():
    # Generate plan and capture output
    rc, out, err = run_cmd("terraform plan -no-color -out=tfplan", check=False)
    if rc != 0:
        print("Terraform plan failed:\n", err)
        sys.exit(rc)
    print(out)
    return out


def apply_if_needed(plan_output):
    # Terraform prints "No changes. Infrastructure is up-to-date." when nothing to do
    if "No changes" in plan_output:
        print("No changes detected – skipping apply.")
        return
    print("Changes detected – applying plan.")
    rc, out, err = run_cmd("terraform apply -auto-approve tfplan", check=True)
    print(out)


def check_iam_role(role_name):
    if not boto3:
        print("boto3 not installed – skipping IAM role check.")
        return True
    iam = boto3.client("iam")
    try:
        iam.get_role(RoleName=role_name)
        print(f"IAM role '{role_name}' exists.")
        return True
    except ClientError as e:
        if e.response["Error"]["Code"] == "NoSuchEntity":
            print(f"IAM role '{role_name}' does NOT exist.")
            return False
        else:
            raise


def import_iam_role_if_missing(role_name):
    if check_iam_role(role_name):
        # Role exists – try to import it into state if not already managed
        rc, out, err = run_cmd(f"terraform state list | grep aws_iam_role.{role_name}")
        if rc == 0:
            print(f"IAM role '{role_name}' already tracked in Terraform state.")
            return
        print(f"Importing IAM role '{role_name}' into Terraform state.")
        run_cmd(f"terraform import aws_iam_role.{role_name} {role_name}", check=True)
    else:
        print(f"Skipping import – role '{role_name}' does not exist in AWS.")


def main():
    # Step 1: ensure formatting
    fmt_check()

    # Step 2: validate configuration
    validate()

    # Step 3: make sure IAM role is tracked (example role name from iam.tf)
    import_iam_role_if_missing("eks_node_role")

    # Step 4: run plan and conditionally apply
    plan_out = plan()
    apply_if_needed(plan_out)


if __name__ == "__main__":
    main()
