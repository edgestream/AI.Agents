# FakeNews Sample Deployment

FakeNews is a mock remote AG-UI agent used by the main backend remoting flow. It returns deterministic structured news content and exposes AG-UI on port `8888`.

## Local Run

Run the sample directly:

```bash
dotnet run --project samples/FakeNews
```

The launch profile binds the agent to `http://localhost:8888`.

## Container Image

CI builds and publishes the sample container as:

```text
ghcr.io/<repository-owner>/agents-samples:<tag>
```

The `/samples/Dockerfile` builds the sample projects and starts the FakeNews agent as the container entrypoint. The container exposes port `8888`.

Build it locally from the repository root:

```bash
docker build -f samples/Dockerfile -t agents-samples:local .
```

Run it locally:

```bash
docker run --rm -p 8888:8888 agents-samples:local
```

## Kubernetes

The manifests in `samples/FakeNews/deploy/k8s` deploy the `agents-samples` image and expose the AG-UI endpoint through the `agents-news` service on port `8888`.

Apply the sample manifests:

```bash
kubectl apply -f samples/FakeNews/deploy/k8s
```

The root `docker-compose.yml` does not include this sample container, and the main CD workflow does not deploy it automatically.
