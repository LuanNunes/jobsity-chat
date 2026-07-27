#!/usr/bin/env bash
# Manual deploy to Cloud Run (same steps as .github/workflows/deploy.yml).
# Requires: gcloud authed with rights on the project; Docker. Reads no secrets
# from the repo — the CloudAMQP URL lives in Secret Manager (rabbitmq-uri).
set -euo pipefail

PROJECT="${PROJECT:-jobsity-chat-2616}"
REGION="${REGION:-us-central1}"
TAG="${TAG:-latest}"
AR="${REGION}-docker.pkg.dev/${PROJECT}/app"

gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet

docker build -f src/api/com.jobsite.chat.Api/Dockerfile -t "${AR}/api:${TAG}" src/api
docker build -f src/api/com.jobsite.chat.Bot/Dockerfile -t "${AR}/bot:${TAG}" src/api
docker build -f src/app/Dockerfile --build-arg NEXT_PUBLIC_API_URL="" -t "${AR}/web:${TAG}" src/app
docker build -f deploy/gateway/Dockerfile -t "${AR}/gateway:${TAG}" deploy/gateway
for s in api bot web gateway; do docker push "${AR}/${s}:${TAG}"; done

gcloud run deploy api --image "${AR}/api:${TAG}" --region "$REGION" --project "$PROJECT" \
  --allow-unauthenticated --min-instances=1 --no-cpu-throttling --port=8080 --timeout=3600 --memory=512Mi \
  --set-env-vars=ASPNETCORE_ENVIRONMENT=Production,ConnectionStrings__ChatDatabase="Data Source=/app/data/jobsity-chat.db" \
  --set-secrets=RabbitMq__Uri=rabbitmq-uri:latest
gcloud run deploy bot --image "${AR}/bot:${TAG}" --region "$REGION" --project "$PROJECT" \
  --no-allow-unauthenticated --min-instances=1 --no-cpu-throttling --port=8080 --memory=512Mi \
  --set-env-vars=ASPNETCORE_ENVIRONMENT=Production --set-secrets=RabbitMq__Uri=rabbitmq-uri:latest
gcloud run deploy web --image "${AR}/web:${TAG}" --region "$REGION" --project "$PROJECT" \
  --allow-unauthenticated --port=8080 --memory=512Mi

API_URL=$(gcloud run services describe api --region "$REGION" --project "$PROJECT" --format='value(status.url)')
WEB_URL=$(gcloud run services describe web --region "$REGION" --project "$PROJECT" --format='value(status.url)')
gcloud run deploy gateway --image "${AR}/gateway:${TAG}" --region "$REGION" --project "$PROJECT" \
  --allow-unauthenticated --port=8080 --timeout=3600 --memory=256Mi \
  --set-env-vars="API_UPSTREAM=${API_URL},API_HOST=${API_URL#https://},WEB_UPSTREAM=${WEB_URL},WEB_HOST=${WEB_URL#https://}"

echo "Public URL: $(gcloud run services describe gateway --region "$REGION" --project "$PROJECT" --format='value(status.url)')"
