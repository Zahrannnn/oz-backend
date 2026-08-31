# Oz Backend

Backend for **Oz**, an Egyptian school uniform retailer moving from paper-based in-store operations to online sales. It is the single source of truth for the platform: a stateless .NET 10 Web API covering the storefront catalog and checkout, admin catalog and order management, Bosta courier shipping, transactional email, background jobs, and audit logging.

The storefront and admin dashboard are separate Next.js apps on Vercel that talk to this API directly — no BFF layer, no server sessions. Every request carries its own auth.

## Features

- **Storefront API** — browse schools → grade stages → products → variants, place orders (atomic and oversell-safe), track or cancel via unguessable order token, subscribe to restock notifications
- **Admin API** — full catalog CRUD (schools, grade stages, item types, products, variants, images), stock edits, whitelisted order state machine, exchanges, Bosta pickup booking, sales/inventory reports, audit log with CSV export
- **Bosta integration** — per-order shipment booking and HMAC-SHA256-verified tracking webhooks (delivered, COD failed, returned)
- **Background jobs** — Hangfire with MSSQL-backed storage: async email delivery, daily auto-cancel of stale orders, restock "notify me" emails
- **Admin auth** — bcrypt hashing, failed-attempt lockout, JWT in an HttpOnly cookie (8h) with Bearer fallback, one-time recovery codes
- **Ops-ready** — structured JSON logging with correlation IDs, security headers, per-route rate limiting, RFC 7807 problem details, health and readiness probes, Hangfire dashboard

## Tech stack

| Layer | Choice |
|---|---|
| Runtime | .NET 10 (ASP.NET Core Web API) |
| ORM | EF Core 10, code-first migrations |
| Database | Microsoft SQL Server |
| Background jobs | Hangfire (MSSQL job store) |
| Auth | JWT bearer + HttpOnly `admin_session` cookie |
| Validation | FluentValidation |
| Email | SMTP (Gmail) |
| Shipping | [Bosta](https://bosta.co) API |
| Hosting | MonsterASP |

## Project structure

```
src/
├── Api/                 # Web API layer
│   ├── Controllers/     # Storefront/, Admin/, and Webhooks/ controllers
│   ├── DTOs/            # Request/response DTOs
│   ├── Services/        # JwtService, BostaClient, SmtpEmailService, AuditLogService
│   ├── Jobs/            # Hangfire jobs (email, auto-cancel, restock alerts)
│   ├── Middleware/      # Correlation IDs, global error handler, security headers
│   └── Validators/      # FluentValidation validators
├── Domain/              # Entities + generic IRepository<T>
└── Infrastructure/      # AppDbContext (entity configs, seed data), EF migrations
docs/                    # PRD, SDD, DB/API design, split API reference (docs/api/)
```

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — `global.json` pins `10.0.301`
- SQL Server — LocalDB (default on Windows) or Docker

### 1. Configure

```bash
cp .env.example .env
```

Fill in the values. ASP.NET Core reads the environment file's `Section__Key` entries directly (e.g. `ConnectionStrings__Default` → `ConnectionStrings:Default`).

> [!IMPORTANT]
> In non-Development environments the app fails fast at startup unless `ConnectionStrings__Default`, `JWT_SECRET`, `BOSTA_API_KEY`, and `BOSTA_WEBHOOK_SECRET` are set. Never commit `.env`.

### 2. Database

The default connection string targets LocalDB. To use Docker instead:

```bash
docker compose up -d   # SQL Server 2022 on localhost:1433
```

Create and apply the schema:

```bash
dotnet ef database update --project src/Infrastructure
```

New migrations are added with:

```bash
dotnet ef migrations add <Name> --project src/Infrastructure
```

### 3. Run

```bash
dotnet run --project src/Api
```

The API listens on <http://localhost:5000>:

| URL | Purpose |
|---|---|
| `GET /api/v1/health` | Liveness probe |
| `GET /api/v1/readyz` | Readiness (DB, Hangfire, config) |
| `/openapi/v1.json` | OpenAPI document |
| `/hangfire` | Hangfire dashboard (localhost only in dev) |

A default admin is seeded on first run with `admin@oz.com` / `admin123`.

> [!WARNING]
> Change the default admin credentials immediately in any shared or production environment.

## Configuration

| Variable | Required | Description |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | ✔ | `Development`, `Staging`, or `Production` |
| `ConnectionStrings__Default` | ✔ | SQL Server connection string |
| `JWT_SECRET` | ✔ | JWT signing key, min 32 chars |
| `BOSTA_API_KEY` | prod | Bosta shipping API key |
| `BOSTA_WEBHOOK_SECRET` | prod | HMAC shared secret for Bosta webhooks |
| `VERCEL_ORIGIN` | – | Extra CORS-allowed origin for the frontend |
| `FRONTEND_URL` | – | Frontend base URL used in tracking links |
| `EMAIL_SMTP_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` / `EMAIL_FROM` | – | SMTP transport; without it emails are logged but not sent |
| `ADMIN_NOTIFY_EMAIL` | – | Admin address for new-order notifications; blank disables them |

## API overview

All endpoints live under `/api/v1`. Admin endpoints accept either the `admin_session` cookie or an `Authorization: Bearer <token>` header (login at `POST /api/v1/admin/auth/login`).

| Area | Highlights |
|---|---|
| Storefront | `GET /schools`, product browsing by school + grade stage, `POST /orders`, `GET /orders/by-token/{token}`, token-based cancel, `POST /variants/{id}/notify-me` |
| Admin | Auth (login, lockout, recovery codes), schools/grade-stages/item-types CRUD, product + variant CRUD with images and stock edits, order list/detail/transitions, exchanges, Bosta pickup, dashboard, reports, audit log with CSV export |
| Webhooks | `POST /webhooks/bosta` — HMAC-verified tracking updates |

Cross-cutting behavior:

- **Errors** — RFC 7807 `application/problem+json`; validation failures return `422` with per-field errors
- **Pagination** — `?page=1&page_size=20` (max 100), responses shaped `{ items, total, page, page_size }`
- **Rate limiting** — public API 60 req/min per IP, checkout 5 req/s, admin 30 req/min; `429` responses include `Retry-After`
- **Idempotency** — `Idempotency-Key` header honored on order placement and stock edits
- **Soft delete only** — archiving via `POST /{id}/archive`; `DELETE` returns `405`
- **Timestamps** — UTC ISO 8601

The full endpoint reference lives in [`docs/API_REFERENCE.md`](docs/API_REFERENCE.md) with per-domain files under [`docs/api/`](docs/api/).

## Design notes

- **Oversell-safe checkout** — `POST /api/v1/orders` runs in a single SQL transaction with `UPDLOCK, ROWLOCK` on variant rows; any stock shortfall rolls back and returns `409 Conflict`
- **Whitelisted order state machine** — transitions are static `{from, to}` pairs; anything else is rejected with `409`
- **Async side effects** — emails and restock alerts always go through Hangfire, never blocking the HTTP response
- **Audit trail** — every state-changing action (order transitions, stock edits, catalog CRUD, logins) is written to the audit log
- **Hangfire self-heals** — the MSSQL-backed job store survives restarts; the recurring auto-cancel job runs daily at 03:00 UTC

## Documentation

- [Product requirements](docs/PRD.md) and [business requirements](docs/BRD.md)
- [Software design document](docs/SDD.md)
- [Database design](docs/DATABASE_DESIGN.md) — schema, indexes, constraints
- [API design](docs/API_DESIGN.md) and [API reference](docs/API_REFERENCE.md)
- [Sequence diagrams](docs/SEQUENCE_DIAGRAMS.md) — order, delivery, exchange, and restock flows
