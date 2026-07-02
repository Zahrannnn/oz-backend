# Backend Team Context

# School Uniform Store Platform

**Audience:** Backend developers (2)
**Stack:** .NET 10 Web API on MonsterASP + MS SQL Server
**Last Updated:** July 2026

---

## Project in One Paragraph

Egyptian school uniform retailer going online. The backend is the single source of truth: REST API, business logic, DB access, Bosta courier integration, Gmail SMTP email transport, Hangfire scheduler, and admin auth. Stateless .NET 10 Web API talking to MS SQL Server, hosted on MonsterASP. Frontend is Next.js 16 on Vercel (separate team). Target: replace paper-based in-store operations.

## Your Team

- 2 backend developers
- Tech Lead: Mohamed Zahran
- Cadence: 2-week sprints, 8 sprints total (16 weeks)
- Owns: .NET 10 API, EF Core, MSSQL schema + migrations, Hangfire, Bosta client, SMTP client, audit log, admin auth

## Tech Stack (Locked)

| Layer | Choice | Notes |
|-------|--------|-------|
| Runtime | .NET 10 (ASP.NET Core Web API) | |
| ORM | EF Core 10 | Code-first migrations |
| Database | Microsoft SQL Server | Hosted on MonsterASP |
| Hosting | MonsterASP | Local `/uploads` for images, no CDN |
| Scheduler | Hangfire | MSSQL-backed job store, in-process |
| Email | Gmail SMTP | Free tier 500/day, Workspace 2000/day |
| Courier | Bosta API | Per-order booking + HMAC webhooks |
| Auth | JWT in `httpOnly; Secure; SameSite=Lax` cookie | Single admin, 8h sliding, no refresh token |
| Validation | FluentValidation | |
| Logging | Serilog → structured JSON to stdout | |
| Tracing | OpenTelemetry | `traceparent` header propagates to Vercel |

## Architecture Rules (Non-Negotiable)

1. **Stateless backend.** Every request carries its own auth. No server session. App restart loses nothing except in-flight Hangfire jobs (MSSQL-backed store recovers schedule).
2. **Single source of truth.** No Next.js API routes, no BFF. The frontend calls you directly.
3. **CORS locked to the Vercel origin.** Configure in `Program.cs`. Reject everything else.
4. **Atomic order placement.** `POST /api/v1/orders` runs inside a single SQL transaction with `WITH (UPDLOCK, ROWLOCK)` on variant rows to prevent concurrent oversell. Rollback on any stock shortfall → `409 Conflict`.
5. **Order state machine is whitelisted.** Static `{fromState, toState}` pairs. Anything else returns `409`.
6. **Audit log on every state-changing action.** Order transitions, stock edits, exchanges, CRUD on catalog, admin login success/failure. Read-only views skip logging.
7. **Hangfire for async side effects.** Email sends, 5-day auto-cancel, notify-me restock trigger. Never block the HTTP response on email.
8. **Idempotent writes.** `POST /orders` and stock edits accept `Idempotency-Key` header.
9. **Soft delete only.** `DELETE` returns `405`. Archiving uses `POST /resource/{id}/archive`.
10. **UTC timestamps.** ISO 8601 with `Z` suffix. Frontend converts to local.
11. **No em dashes** in error messages or audit log fields.

## Conventions

- **Base URL:** `https://api.<domain>/api/v1`
- **Versioning:** All paths prefixed with `/api/v1/`. Breaking changes bump to `/v2/`.
- **Field casing:** `camelCase` in JSON.
- **Error format:** RFC 7807 `application/problem+json`. Shape: `{ type, title, status, detail, instance }`.
- **Pagination:** `?page=1&page_size=20` (max 100). Response: `{ items, total, page, page_size }`.
- **Filtering:** `?field=value`; comma-separated for OR (`?state=placed,ready_to_ship`).
- **Sorting:** `?sort=created_at` (asc) or `?sort=-created_at` (desc).
- **Auth cookie name:** `admin_session`.
- **Admin JWT claims:** `sub` (admin id), `role` = `admin`, `iat`, `exp` (8h from issue, sliding).
- **Password hashing:** bcrypt (cost 12) or argon2.
- **Token URL:** SHA2-256 hash of the raw token stored in `orders.token_hash`. Raw token never persisted. Compare via hash.

## Database (You Own the Schema)

14 tables. Full spec in `docs/DATABASE_DESIGN.md`. High-level:

- `school`, `grade_stage`, `item_type` — catalog anchors
- `product` (school_id + grade_stage_id + item_type_id + gender + color + is_in_set)
- `variant` (product_id, size_label, price_incl_vat, stock, low_stock_threshold)
- `product_image` (product_id, url, sort_order)
- `orders` (token_hash, state, channel, customer fields, total, bosta_tracking_id, timestamps)
- `order_item` (order_id, variant_id, qty, unit_price_snapshot, line_total_snapshot)
- `exchange` (order_item_id, old_variant_id, new_variant_id, qty, price_delta, reason)
- `pending_alert` (variant_id, email, email_hash, notified, created_at, notified_at)
- `admin` (email, password_hash, password_salt, failed_attempts, locked_until)
- `password_recovery` (admin_id, code_hash, expires_at, used)
- `audit_log` (actor_id, action, entity_type, entity_id, before_json, after_json, reason, created_at)
- `email_log` (order_id, template, to_email, status, sent_at, error)
- Hangfire schema (managed by Hangfire, MSSQL-backed)

**Migrations:** EF Core, forward-only. Applied by CI pipeline in the deploy step. No rollback migrations in v1; rollback = restore DB from backup.

## Key Flows You Own

1. **Place order** — atomic tx, UPDLOCK, stock check, deduct, INSERT orders + items, audit log, enqueue email
2. **Order state machine** — whitelisted transitions, 5-day auto-cancel Hangfire job
3. **Bosta integration** — per-order booking, HMAC-verified webhooks (delivered, COD-failed, returned)
4. **Notify-me restock trigger** — on stock edit, detect 0→positive diff, enqueue email job, mark `notified=true`
5. **Exchange** — multi-step tx: refund old variant stock, take new variant stock, compute price delta, update order total, audit log
6. **Admin auth** — bcrypt verify, failed-attempts lockout (5 attempts → 15-min lock), JWT cookie, one-time recovery code
7. **Audit log** — writer on every state mutation, reader with filters + CSV export

## External Integrations

| Integration | Direction | Auth | Notes |
|-------------|-----------|------|-------|
| Bosta API | Outbound (create shipment) | API key in header | Per-order booking, no batch |
| Bosta webhook | Inbound (tracking updates) | HMAC-SHA256 shared secret | Verify before processing; idempotent |
| Gmail SMTP | Outbound (email) | SMTP auth (user + app password) | Order updates, notify-me, auto-cancel |
| Vercel revalidate | Outbound (on-demand ISR) | `REVALIDATE_SECRET` env | Fires after admin product CRUD |

## Frontend Dependencies (When FE Blocks on You)

| FE needs | You provide | Task |
|----------|-------------|------|
| Schools list | `GET /api/v1/schools` | BE-001 |
| Products by school+grade | `GET /api/v1/schools/{id}/grades/{id}/products` | BE-014 |
| Product detail | `GET /api/v1/products/{id}` | BE-015 |
| Place order | `POST /api/v1/orders` | BE-020 |
| Order by token | `GET /api/v1/orders/by-token/{token}` | BE-021 |
| Notify-me | `POST /api/v1/variants/{id}/notify-me` | BE-031 |
| Admin login | `POST /api/v1/admin/auth/login` | BE-040 |
| Admin CRUD | `/api/v1/admin/...` | BE-050+ |
| Stock edit | `PUT /api/v1/admin/variants/{id}/stock` | BE-060 |
| Orders list | `GET /api/v1/admin/orders` | BE-070 |
| Bosta pickup | `POST /api/v1/admin/orders/{id}/bosta-pickup` | BE-072 |
| Mark picked up | `POST /api/v1/admin/orders/{id}/mark-picked-up` | BE-074 |
| Exchange | `POST /api/v1/admin/orders/{id}/exchanges` | BE-076 |
| Reports | `GET /api/v1/admin/reports/{type}` | BE-080 |
| Audit log | `GET /api/v1/admin/audit-log` | BE-090 |

## How to Verify Your Work

1. **Build:** `dotnet build` (must succeed before PR)
2. **Tests:** `dotnet test` (xUnit, target 80%+ coverage on business logic)
3. **Migrations:** `dotnet ef database update` against localdb
4. **Run locally:** `docker-compose up` (SQL Server container + .NET API)
5. **API smoke test:** Hit `GET /api/v1/health` → 200; `GET /api/v1/readyz` → 200 (DB + Hangfire + Bosta key configured)
6. **Postman:** Run the collection against your local instance
7. **Hangfire dashboard:** `http://localhost:5000/hangfire` (locked to localhost in dev)

## Reference Docs (Read These)

- `docs/PRD.md` — functional requirements, business rules, edge cases
- `docs/SDD.md` — architecture, deep dives on critical components
- `docs/DATABASE_DESIGN.md` — full schema, indexes, constraints
- `docs/API_DESIGN.md` — every endpoint you'll implement
- `docs/SPRINT_PLAN.md` — sprint themes and deliverables
- `docs/USE_CASE_DIAGRAM.md` — actor→use case mapping
- `docs/SEQUENCE_DIAGRAMS.md` — interaction flows (sd-01 through sd-12)

## Anti-References (Don't Build These)

- Parent accounts / auth
- Online payments (Paymob interface reserved for v2, not implemented)
- Refunds (only stock refunds on cancel/return; no cash refunds)
- SMS / WhatsApp / push notifications
- Batch Bosta pickup booking
- Customer self-service exchange button
- Email-based password reset link (one-time recovery code only)
- Refresh token rotation
- Redis cache layer (ISR handles catalog reads; add Redis only if dashboard p95 > 1s)
- Read replicas (single MSSQL instance for v1)
- Multi-store / multi-city (single store, single city)
