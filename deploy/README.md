# Deployment

The app runs on **Google Cloud Run** behind a single **Caddy gateway** (one HTTPS
origin → `/api` + `/hubs` to the api service, everything else to web), with a
free **CloudAMQP** broker and ephemeral SQLite. Local dev is unchanged — use
`docker compose up` / `make up` (see the root README).

Live demo: the gateway's `*.run.app` URL (from `gcloud run services describe gateway`).

## Auto-deploy (CI)
`.github/workflows/deploy.yml` builds and deploys all four services on every push
to `main`. Auth is **keyless** via Workload Identity Federation — GitHub Actions
impersonates the `gh-deploy` service account through the `github` WIF provider
(locked to this repo). **No service-account key is stored anywhere.**

## Manual deploy
`deploy/deploy.sh` runs the same steps by hand (needs `gcloud` authed with rights
on the project). It reads nothing secret from the repo.

## One-time setup (already done for project `jobsity-chat-2616`, region `us-central1`)
1. Project + billing; enable `run`, `artifactregistry`, `cloudbuild`, `secretmanager`, `orgpolicy`.
2. Artifact Registry repo `app`.
3. Secret Manager `rabbitmq-uri` = the CloudAMQP `amqps://…` URL (never in git); runtime SA granted `secretAccessor`.
4. Project-scoped org-policy exception on `iam.allowedPolicyMemberDomains` (`allowAll`) so Cloud Run services can be public — scoped to this project only.
5. WIF pool/provider `github` + `gh-deploy` SA (`run.admin`, `artifactregistry.writer`, `iam.serviceAccountUser`); repo bound via `iam.workloadIdentityUser`.

## Security notes
- The CloudAMQP URL lives only in Secret Manager and is injected as `RabbitMq__Uri` at deploy.
- RabbitMQ is reached over `amqps` (TLS); the Identity cookie is `Secure` in Production.
- The gateway is the single public surface; api/web are also public only so the gateway can proxy to them (auth-gated endpoints still require the cookie).
- Tear down with `gcloud run services delete api bot web gateway` or delete the project.
