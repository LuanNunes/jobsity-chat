# Jobsity Chat — Frontend

Minimal Next.js (App Router) + TypeScript + React client for the chat API.
Talks to the ASP.NET Core backend over cookie-authenticated `fetch` and
`@microsoft/signalr`.

## Run

The backend API must be running first (default `http://localhost:5202`):

```bash
# from the repo root
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5202 \
  dotnet run --project src/com.jobsite.chat.Api
```

Then the frontend:

```bash
cd frontend
npm install
npm run dev        # http://localhost:3000
```

Register at `/register`, then chat at `/chat`. The API's CORS policy allows the
`http://localhost:3000` origin with credentials, so the auth cookie flows to
both the REST endpoints and the SignalR hub.

## Configuration

`NEXT_PUBLIC_API_URL` (default `http://localhost:5202`) — the API base URL.
Copy `.env.local.example` to `.env.local` to override.

## Structure

- `app/login`, `app/register` — auth forms (`POST /api/auth/login|register`).
- `app/chat` — room list (`/api/rooms`), message pane, composer; joins a room
  over SignalR (`/hubs/chat`), renders `LoadHistory` + `ReceiveMessage`, and
  shows the ephemeral stock-command ack/reject.
- `lib/api.ts` — typed `fetch` wrapper (`credentials: "include"`).
- `lib/chatConnection.ts` — SignalR hub connection + client event handlers.
