# Frontend image — Next.js standalone. Build context is src/app (see docker-compose.yml).
FROM node:22-alpine AS build
WORKDIR /app

# Install deps against the lockfile first for layer caching.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# NEXT_PUBLIC_* are inlined into the client bundle at BUILD time, so the API URL
# the browser calls must be supplied here (defaults to the dockerized Api on the host).
ARG NEXT_PUBLIC_API_URL=http://localhost:5080
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000

# Next.js "standalone" output: a minimal self-contained server + its static assets.
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static

USER node
EXPOSE 3000
CMD ["node", "server.js"]
