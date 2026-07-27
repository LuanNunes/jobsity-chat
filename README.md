# Jobsity Chat

A real-time chat application where registered users talk in chat rooms and a
`/stock=<code>` command is answered by a **decoupled bot** that fetches a quote
from [stooq.com](https://stooq.com), parses it, and posts the result back through
RabbitMQ. Built backend-first with .NET 10 (Clean Architecture) and a minimal
Next.js frontend.

## Features

**Mandatory**
- Registered users log in and chat in real time (ASP.NET Identity cookie auth + SignalR).
- `/stock=<stock_code>` command (e.g. `/stock=aapl.us`).
- A decoupled **Bot** worker calls `https://stooq.com/q/l/?s=<code>&f=sd2t2ohlcv&h&e=csv`.
- The Bot parses the CSV and posts back over **RabbitMQ** as the bot:
  `"AAPL.US quote is $93.42 per share"`.
- Messages are ordered by timestamp; only the **last 50** are shown.
- Unit + integration tests (xUnit backend, Vitest frontend).

**Bonus**
- Multiple chat rooms (with live, no-refresh room broadcasting).
- **.NET Identity** for authentication.
- Unknown commands and bot/API failures are handled gracefully.
- **One-command installer** (`docker compose` / `make install`).

## Architecture

Clean Architecture; dependencies point inward — **Domain ← Shared ← Service ← Repository ← Api**, and **Bot → Shared**.

| Project | Role |
|---|---|
| `Domain` | Entities, value objects, DTOs, enums, domain rules (FluentValidation) — no infra |
| `Shared` | Interfaces (`Contracts`), RabbitMQ messaging, persistence keys, logging |
| `Service` | Application layer — MediatR commands/queries |
| `Repository` | EF Core + SQLite, two DbContexts, migrations |
| `Api` | Minimal-API host + SignalR hub (JSON only, no MVC/Razor), vertical slices |
| `Bot` | Worker that fetches/parses stock quotes and replies over RabbitMQ |

**Flow:** the SPA sends messages over SignalR → the Api parses `/stock=` and publishes a request to RabbitMQ (stock commands are *not* saved as messages) → the Bot fetches + parses the quote and publishes a reply → the Api's reply consumer re-posts it as the bot and broadcasts it to the room.

**Tech:** .NET 10, ASP.NET Core minimal APIs, SignalR, EF Core/SQLite, ASP.NET Identity, MediatR, FluentValidation, Polly, RabbitMQ, Serilog · Next.js 15 / React 19 / TypeScript.

## Quick start (installer)

**Prerequisite:** Docker (with Compose). One command builds and runs everything — RabbitMQ, API, Bot, and the web UI:

```bash
make install         # = docker compose up --build -d
```

Then open:

| | URL |
|---|---|
| Web app | http://localhost:3000 |
| API | http://localhost:5080 (`/health`, `/health/ready`) |
| RabbitMQ UI | http://localhost:15672 (guest / guest) |

Open two browser windows, register two users, join the same room, chat, and try `/stock=aapl.us`.

Other targets: `make down` (stop), `make clean` (stop + drop the DB volume), `make logs`, `make ps`, `make test`. (`docker compose up --build` works directly if you don't have `make`.)

## Local development

Run the backend from an IDE/CLI and the frontend with the Next dev server; keep RabbitMQ in Docker:

```bash
docker compose up -d rabbitmq                       # message broker
dotnet run --project src/api/com.jobsite.chat.Api   # API on http://localhost:5202
dotnet run --project src/api/com.jobsite.chat.Bot   # stock bot
cd src/app && npm install && npm run dev            # web on http://localhost:3000
```

The frontend defaults its API base to `http://localhost:5202` (override with `NEXT_PUBLIC_API_URL`). The API applies EF migrations on startup.

## Testing

```bash
make test                                   # backend + frontend
dotnet test src/api/com.jobsite.chat.slnx   # backend (xUnit)
cd src/app && npx vitest run                # frontend (Vitest + RTL)
```

## Project structure

```
src/
  app/                     # Next.js frontend (login, register, chat)
  api/                     # .NET solution (com.jobsite.chat.slnx)
    com.jobsite.chat.{Domain,Shared,Service,Repository,Api,Bot}/
    tests/com.jobsite.chat.Tests/
docker-compose.yml         # rabbitmq + api + bot + web
Makefile
```

## Configuration

Runtime config is via `appsettings.json` and environment variables (Compose sets them):
`ConnectionStrings__ChatDatabase`, `RabbitMq__{Host,Port,UserName,Password}`, `Cors__AllowedOrigin`,
`RateLimiting__{PermitPerMinute,AuthPermitPerMinute}`. Design-time EF migrations read the connection
string from `JOBSITY_CHAT_CONNECTION_STRING`.

> The RabbitMQ credentials in this repo are the local `guest/guest` defaults for development only.
> For a real deployment, supply them (and the connection string) via environment/secrets, not source control.
