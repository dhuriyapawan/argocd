# Cloud Voting Platform

A modern, multi-tier voting application engineered for cloud-native deployment. Users can choose between two options (Cats vs Dogs) on a frontend interface, which buffer votes in Redis. A background worker pulls these votes, processes them, and records them in PostgreSQL. A real-time Node.js dashboard reads the results and pushes dynamic live updates to clients via WebSockets.

## Architecture & Workflow

```
                    Developer
                        │
                        │ Push Code
                        ▼
                  GitHub Repository
                        │
              GitHub Actions CI/CD
                        │
        ┌───────────────┴────────────────┐
        │                                │
   Build Docker Images             Run Tests
        │
        ▼
   Push Images to AWS ECR
        │
        ▼
 Update Helm Values (image tag)
        │
        ▼
 GitOps Repository (ArgoCD)
        │
        ▼
 Kubernetes Cluster
        │
 ┌──────┴──────────┬─────────────┬────────────┐
 │                 │             │            │
Vote Service   Worker Service Result Service Redis
                                      │
                                      │
                                  PostgreSQL
```

- **Vote Service**: Python/Flask frontend that serves a modern UI for users to cast their choice.
- **Redis**: In-memory message broker to queue votes.
- **Worker Service**: .NET Core background service that pops votes from Redis and upserts them into Postgres.
- **PostgreSQL**: Relational database to persist the voting data.
- **Result Service**: Node.js/Express frontend showcasing live results via Socket.io.

---

## Local Development (Docker Compose)

Start the stack locally:

```bash
cd cloud-voting-platform
docker-compose up --build
```

- **Vote Application**: [http://localhost:5000](http://localhost:5000)
- **Result Application**: [http://localhost:5001](http://localhost:5001)

Stop the stack:

```bash
docker-compose down -v
```

---

## Kubernetes Deployment

### Option 1: Helm Deployment

1. Make sure you are in the helm chart directory:
   ```bash
   cd cloud-voting-platform/helm
   ```
2. Install the Helm Chart:
   ```bash
   helm install voting-app ./voting-app
   ```
3. Upgrade the Helm Chart:
   ```bash
   helm upgrade voting-app ./voting-app
   ```

### Option 2: Raw Kubernetes Manifests

Apply the combined manifest file:

```bash
kubectl apply -f cloud-voting-platform/manifests/voting-app.yaml
```

---

## CI/CD & GitOps Integration

### 1. GitHub Actions (CI)
The pipeline is located in [ci.yaml](file:///.github/workflows/ci.yaml). It automatically triggers on commits to the `main` branch affecting any of the microservices.
It does the following:
- Logs in to AWS ECR using GitHub Repository secrets (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`).
- Builds and pushes the Docker images.
- Updates the image tags inside `helm/voting-app/values.yaml` with the Git Commit SHA.
- Commits and pushes the change back to the repository.

### 2. ArgoCD (GitOps CD)
ArgoCD detects the change in the Helm chart's `values.yaml` and synchronizes the state to the Kubernetes cluster automatically.
The ArgoCD Application definition is located at [application.yaml](file:///cloud-voting-platform/argocd/application.yaml).

Apply it in your cluster:
```bash
kubectl apply -f cloud-voting-platform/argocd/application.yaml
```

---

## Monitoring Setup

### Prometheus
Configured to monitor resource consumption and scraping intervals:
- [prometheus.yml](file:///cloud-voting-platform/monitoring/prometheus/prometheus.yml)

### Grafana Dashboard
Import the JSON dashboard config to see live CPU, Memory, and Network I/O metrics:
- [dashboard.json](file:///cloud-voting-platform/monitoring/grafana/dashboard.json)
