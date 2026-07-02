# Backend Task List

# School Uniform Store Platform

**Team:** Backend (2 devs)
**Cadence:** 2-week sprints
**Total Tasks:** 52
**Estimate Convention:** story points (1 SP ≈ 1 dev-day)

---

## How to Read This

Each task has: **ID**, **title**, **sprint**, **estimate** (SP), **dependencies** (task IDs), **acceptance criteria**. Tasks are grouped by sprint. Pick up the lowest-numbered unblocked task in the current sprint. Mark a task done only when all acceptance criteria pass and `dotnet test` is green.

---

# Sprint 0 — Foundation & Setup (Weeks 1–2)

## BE-001 — Scaffold .NET 10 Web API
**SP:** 2
**Dependencies:** none
**Acceptance:**
- `dotnet new webapi` with controllers, .NET 10
- Solution + project structure: `src/Api/`, `src/Domain/`, `src/Infrastructure/`
- `dotnet build` succeeds
- `dotnet run` starts server on `http://localhost:5000`
- `GET /api/v1/health` returns 200 `{ status: "ok" }`

## BE-002 — EF Core + SQL Server connection
**SP:** 2
**Dependencies:** BE-001
**Acceptance:**
- EF Core 10 NuGet packages installed
- `AppDbContext` with `SqlServer` provider
- Connection string from env var `ConnectionStrings__Default`
- `dotnet ef database update` runs against localdb
- `docker-compose.yml` with SQL Server container for local dev

## BE-003 — Repository pattern + DTOs + FluentValidation
**SP:** 3
**Dependencies:** BE-001
**Acceptance:**
- Generic `IRepository<T>` + `Repository<T>` base
- DTO layer separate from entities (Mapster or manual mapping)
- FluentValidation pipeline in MediatR or controller layer
- Sample entity end-to-end: entity → repository → DTO → controller → validation

## BE-004 — Hangfire setup
**SP:** 1
**Dependencies:** BE-002
**Acceptance:**
- Hangfire server registered in `Program.cs`
- MSSQL-backed job storage (uses the same connection string)
- Dashboard at `/hangfire` (locked to localhost in dev)
- Recurring job placeholder: `Console.WriteLine` every minute

## BE-005 — CORS + security headers + error format
**SP:** 2
**Dependencies:** BE-001
**Acceptance:**
- CORS policy allows only `VERCEL_ORIGIN` env var
- Security headers: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Strict-Transport-Security`, `Content-Security-Policy`
- Global exception handler returns RFC 7807 `application/problem+json`
- Validation errors return 422 with field-level details

## BE-006 — First migration: school, grade_stage, item_type
**SP:** 2
**Dependencies:** BE-002
**Acceptance:**
- EF Core entities for `school`, `grade_stage`, `item_type`
- Migration created and applies cleanly
- Seed data: 3 schools, 9 grade-stages, 10 item-types
- Unique indexes: `grade_stage(school_id, name)`, `item_type(name)`

## BE-007 — Health + readiness endpoints
**SP:** 1
**Dependencies:** BE-002, BE-004
**Acceptance:**
- `GET /api/v1/health` → 200 if process alive (no DB call)
- `GET /api/v1/readyz` → 200 only if (a) MSSQL reachable, (b) Hangfire schema initialized, (c) Bosta API key env var set
- 503 if any readiness check fails

---

# Sprint 1 — Catalog Core (Weeks 3–4)

## BE-010 — Schools endpoints
**SP:** 2
**Dependencies:** BE-006
**Acceptance:**
- `GET /api/v1/schools` → list (only `is_active = true`)
- `GET /api/v1/schools?q=...` → `WHERE name LIKE '%' + @q + '%' COLLATE Arabic_CI_AS`
- `GET /api/v1/schools/{id}` → single school or 404
- Pagination on list endpoint
- Response: `{ items, total, page, page_size }`

## BE-011 — Grade-stage endpoints
**SP:** 2
**Dependencies:** BE-006
**Acceptance:**
- `GET /api/v1/schools/{id}/grades` → grade-stages for school
- Ordered by `sort_order`
- 404 if school doesn't exist

## BE-014 — Products by school + grade
**SP:** 4
**Dependencies:** BE-012
**Acceptance:**
- `GET /api/v1/schools/{schoolId}/grades/{gradeId}/products`
- Returns products with variants (stock, price) and first image
- Filters: `?item_type=`, `?gender=`
- Response shape: `{ items: [{ id, name, slug, is_in_set, variants: [...], image: url }] }`
- Index on `product(school_id, grade_stage_id, is_archived)`

## BE-015 — Product detail endpoint
**SP:** 2
**Dependencies:** BE-014
**Acceptance:**
- `GET /api/v1/products/{id}` → full detail
- Includes: all variants, all images (sorted by `sort_order`), school + grade + item-type names
- 404 if product archived or not found

## BE-012 — Product + variant + image migration
**SP:** 3
**Dependencies:** BE-006
**Acceptance:**
- Entities: `product`, `variant`, `product_image`, `product_grade`
- Migration applies cleanly
- FKs: `product.school_id`, `product.grade_stage_id`, `product.item_type_id`, `variant.product_id`, `product_image.product_id`
- Indexes: `variant(product_id)`, `product_image(product_id, sort_order)`, `product(school_id, grade_stage_id, is_archived)`

## BE-013 — Image upload endpoint
**SP:** 3
**Dependencies:** BE-012
**Acceptance:**
- `POST /api/v1/admin/products/{id}/images` (multipart/form-data)
- Saves to `/uploads/products/{productId}/{guid}.{ext}`
- Returns `{ id, url, sort_order }`
- `DELETE /api/v1/admin/products/{id}/images/{imageId}` removes file + row
- Max file size: 5 MB; allowed: jpg, png, webp
- Admin auth required

## BE-050 — Admin: school CRUD
**SP:** 3
**Dependencies:** BE-006, BE-040
**Acceptance:**
- `POST /api/v1/admin/schools` → create
- `PUT /api/v1/admin/schools/{id}` → update
- `POST /api/v1/admin/schools/{id}/archive` → soft delete (set `is_active = false`)
- `DELETE` returns 405
- Audit log written on each mutation
- Admin auth required

## BE-052 — Admin: product + variant CRUD
**SP:** 4
**Dependencies:** BE-012, BE-040
**Acceptance:**
- `POST/PUT /api/v1/admin/products` → create / update
- `POST /api/v1/admin/products/{id}/archive`
- `POST/PUT/DELETE /api/v1/admin/products/{id}/variants` → variant CRUD
- Audit log on each mutation
- Admin auth required

---

# Sprint 2 — Cart + Checkout (Weeks 5–6)

## BE-019 — Orders + order_items migration
**SP:** 3
**Dependencies:** BE-012
**Acceptance:**
- Entities: `orders`, `order_item`
- `orders.token_hash` (SHA2-256), unique index
- `orders.state` (CHECK constraint with allowed values)
- `orders.channel` (`delivery` or `pickup`)
- `order_item.order_id`, `order_item.variant_id` FKs
- `order_item.unit_price_snapshot`, `order_item.line_total_snapshot` (immutable snapshots)
- Indexes: `orders(state, state_changed_at)`, `orders(token_hash)`, `order_item(order_id)`

## BE-020 — Place order endpoint (atomic tx)
**SP:** 5
**Dependencies:** BE-019
**Acceptance:**
- `POST /api/v1/orders` with `Idempotency-Key` header
- Body: `{ channel, customer: { name, phone, email, address }, items: [{ variantId, qty }] }`
- Flow:
  1. BEGIN TRANSACTION
  2. For each item: `SELECT stock FROM variant WITH (UPDLOCK, ROWLOCK) WHERE id = @id`
  3. Check `stock >= qty`; if any fails → ROLLBACK, return 409 with offending variantId
  4. INSERT `orders` (state = `placed`, token_hash = SHA2-256(random token), customer fields, total)
  5. INSERT `order_item` rows with price snapshots
  6. UPDATE `variant SET stock = stock - qty`
  7. INSERT `audit_log`
  8. COMMIT
  9. Enqueue `SendOrderConfirmationEmail` Hangfire job
  10. Return 201 `{ orderId, token, tokenUrl, total }`
- Concurrency test: 2 parallel requests for last item → only one succeeds

## BE-021 — Order by token endpoint
**SP:** 2
**Dependencies:** BE-020
**Acceptance:**
- `GET /api/v1/orders/by-token/{token}`
- Hashes token with SHA2-256, looks up by `token_hash`
- Returns: order, items, timeline (state history from audit log)
- 404 if token doesn't match
- No timing side-channel (constant-time comparison)

## BE-022 — SMTP service (Gmail)
**SP:** 3
**Dependencies:** BE-004
**Acceptance:**
- `IEmailService` interface + `SmtpEmailService` implementation
- Gmail SMTP: `smtp.gmail.com:587`, STARTTLS, app password from env
- `SendOrderConfirmationEmail` Hangfire job: renders HTML template, sends to `customer_email`
- Retries: 3 attempts with exponential backoff
- `email_log` row written on each send (success or failure)
- Template includes: order items, total, token URL, "save this link" message

---

# Sprint 3 — Admin Auth + Management (Weeks 7–8)

## BE-039 — Admin + password_recovery + audit_log + pending_alert migration
**SP:** 3
**Dependencies:** BE-002
**Acceptance:**
- Entities: `admin`, `password_recovery`, `audit_log`, `pending_alert`
- `admin`: `email` (unique), `password_hash`, `password_salt`, `failed_attempts`, `locked_until`, `last_login_at`
- `password_recovery`: `admin_id`, `code_hash`, `expires_at`, `used`, `attempts`
- `audit_log`: `actor_id`, `action`, `entity_type`, `entity_id`, `before_json`, `after_json`, `reason`, `created_at`; indexes `(created_at DESC)`, `(actor_id, created_at DESC)`
- `pending_alert`: `variant_id`, `email`, `email_hash`, `notified`, `created_at`, `notified_at`; unique on `(variant_id, email_hash) WHERE notified = 0`

## BE-040 — Admin login endpoint
**SP:** 4
**Dependencies:** BE-039
**Acceptance:**
- `POST /api/v1/admin/auth/login` body `{ email, password }`
- bcrypt verify (cost 12)
- On success: `failed_attempts = 0`, `last_login_at = NOW`, generate JWT (sub, role, 8h exp), set `admin_session` cookie (`httpOnly; Secure; SameSite=Lax; Path=/; Max-Age=28800`)
- Audit log: `admin.login_success`
- On failure: increment `failed_attempts`; if ≥5, set `locked_until = NOW + 15min`; return 401
- On locked (locked_until > NOW): return 423
- No email enumeration: same error for wrong email vs wrong password

## BE-041 — JWT middleware + auth guard
**SP:** 2
**Dependencies:** BE-040
**Acceptance:**
- Middleware reads `admin_session` cookie, validates JWT
- `[Authorize]` attribute on all `/api/v1/admin/*` controllers
- Invalid / expired token → 401
- Sliding renewal: each successful admin request re-issues cookie with fresh 8h exp
- `GET /api/v1/admin/auth/me` → returns admin profile

## BE-042 — Password recovery (one-time code)
**SP:** 3
**Dependencies:** BE-040
**Acceptance:**
- `POST /api/v1/admin/auth/forgot-password` body `{ email }`
- Always returns 200 (no enumeration)
- If email exists: generate 6-digit crypto-random code, hash with SHA-256, store in `password_recovery` (expires_at = NOW + 5min), audit log
- Code is displayed in-app (per interview decision: no email reset link in v1). For v1 single-admin, the code is shown on the admin setup screen.
- `POST /api/v1/admin/auth/verify-recovery-code` body `{ email, code }`
- On match + not expired: set `used = true`, generate JWT, set cookie, return 200
- On expired: return 410
- On mismatch: increment `attempts`, return 401

## BE-044 — Dashboard endpoint
**SP:** 3
**Dependencies:** BE-041
**Acceptance:**
- `GET /api/v1/admin/dashboard`
- Returns: `{ revenue_this_month, orders_today, pending_orders, low_stock_count, recent_activity: [...], low_stock_variants: [...] }`
- Aggregation queries indexed (use `orders(created_at)`, `variant(stock, low_stock_threshold)`)
- p95 < 500ms

## BE-056 — Admin: grade-stage + item-type CRUD
**SP:** 2
**Dependencies:** BE-006, BE-040
**Acceptance:**
- CRUD for `grade_stage` (per school) and `item_type` (shared)
- Same patterns as BE-050
- Audit log on mutations

## BE-060 — Stock edit endpoint (diff trigger)
**SP:** 4
**Dependencies:** BE-039, BE-041
**Acceptance:**
- `PUT /api/v1/admin/variants/{id}/stock` body `{ stock, reason?, threshold? }`
- Flow:
  1. BEGIN TRANSACTION
  2. `SELECT stock FROM variant WITH (UPDLOCK, ROWLOCK) WHERE id = @id` → `oldStock`
  3. UPDATE stock (and threshold if provided)
  4. INSERT `audit_log` (before: oldStock, after: newStock)
  5. COMMIT
  6. Outside tx: if `oldStock == 0 AND newStock > 0` → enqueue `SendNotifyMeEmailsJob(variantId)`
  7. Outside tx: if `newStock < threshold` → upsert `low_stock_cache`
- Returns updated variant
- `Idempotency-Key` supported

## BE-090 — Audit log endpoints
**SP:** 3
**Dependencies:** BE-039, BE-041
**Acceptance:**
- `GET /api/v1/admin/audit-log?actor=&action=&from=&to=&entity_type=&page=&page_size=`
- Filterable, paginated, sorted by `created_at DESC`
- `GET /api/v1/admin/audit-log/export` → CSV download (same filters, no pagination)
- Read-only

---

# Sprint 4 — Orders + Bosta (Weeks 9–10)

## BE-069 — Bosta tracking fields migration
**SP:** 1
**Dependencies:** BE-019
**Acceptance:**
- Add to `orders`: `bosta_tracking_id` (nvarchar, unique index), `handed_to_courier_at`, `in_transit_at`, `delivered_at`, `returned_at`, `cod_failed_at`
- All timestamps nullable datetime2

## BE-070 — Admin orders list endpoint
**SP:** 3
**Dependencies:** BE-041
**Acceptance:**
- `GET /api/v1/admin/orders?state=&school=&from=&to=&search=&page=&page_size=`
- `search` matches `customer_phone` (LIKE) or exact `id`
- Returns: `{ items: [{ id, state, channel, customer_name, customer_phone, total, created_at }], total, page, page_size }`
- Indexes: `orders(state, created_at DESC)`, `orders(customer_phone)`

## BE-071 — Admin order detail endpoint
**SP:** 2
**Dependencies:** BE-070
**Acceptance:**
- `GET /api/v1/admin/orders/{id}`
- Returns: order, items, exchanges, state history (from audit log), bosta_tracking_id
- 404 if not found

## BE-071a — Order state machine transition endpoint
**SP:** 3
**Dependencies:** BE-071
**Acceptance:**
- `POST /api/v1/admin/orders/{id}/transition` body `{ toState }`
- Static whitelist of `{fromState, toState}` pairs (per PRD Appendix B)
- Invalid pair → 409 with `{ error: "Invalid transition", from, to }`
- On success: UPDATE state, `state_changed_at = NOW`, INSERT audit_log
- Sliding timestamps: `handed_to_courier_at`, `delivered_at`, etc. set on corresponding transitions

## BE-072 — Bosta pickup booking endpoint
**SP:** 4
**Dependencies:** BE-069, BE-071a
**Acceptance:**
- `POST /api/v1/admin/orders/{id}/bosta-pickup`
- Guard: order must be in `ready_to_ship`, channel = `delivery`
- Call `BostaClient.CreateShipment(orderId, customer, address, codAmount)` (API key in header)
- On 2xx: store `bosta_tracking_id`, transition to `handed_to_courier`, set `handed_to_courier_at`, audit log, enqueue `OrderShippedEmail`
- On non-2xx / timeout: return 502, no state change, admin retries
- `IBostaClient` interface (for future providers + testing)

## BE-073 — Bosta webhook handler
**SP:** 4
**Dependencies:** BE-072
**Acceptance:**
- `POST /api/v1/webhooks/bosta`
- Verify HMAC-SHA256 signature header against shared secret; 401 on mismatch
- Parse `{ trackingId, status }`
- Map statuses:
  - `in_transit` → if state = `handed_to_courier`, advance to `in_transit`, set `in_transit_at`
  - `delivered` → transition to `delivered`, set `delivered_at`, enqueue `OrderDeliveredEmail`, then `closed_success`
  - `cod_failed` → transition to `cod_failed`, set `cod_failed_at`, enqueue `CodFailedEmail`
  - `returned_to_store` → transition to `returned_to_store`, refund stock per item, `closed_failed`, enqueue `ReturnEmail`
- Idempotent: re-delivery of same status is no-op (state already advanced)

## BE-074 — Mark picked up endpoint
**SP:** 2
**Dependencies:** BE-071a
**Acceptance:**
- `POST /api/v1/admin/orders/{id}/mark-picked-up`
- Guard: state = `ready_for_pickup`, channel = `pickup`
- Transitions: `ready_for_pickup → picked_up → closed_success`
- Sets `picked_up_at`
- Audit log
- Stock NOT decremented again (already done at `placed`)

## BE-075 — Auto-cancel Hangfire job
**SP:** 3
**Dependencies:** BE-071a, BE-004
**Acceptance:**
- Recurring job: daily at 03:00 server time
- Query: `SELECT id FROM orders WHERE state IN ('placed','ready_to_ship','ready_for_pickup') AND state_changed_at < DATEADD(day, -5, GETUTCDATE())`
- For each: BEGIN TX → UPDATE state = `cancelled`, `cancelled_at = NOW` → refund stock per item → INSERT audit_log (actor = system) → COMMIT → enqueue `OrderCancelledEmail`
- Idempotent: re-run skips already-cancelled
- On-demand trigger: `POST /api/v1/admin/jobs/run-auto-cancel` (admin-only, for testing)

## BE-075a — Order update email jobs
**SP:** 2
**Dependencies:** BE-022
**Acceptance:**
- `OrderShippedEmail` job (with Bosta tracking ID + token URL)
- `OrderDeliveredEmail` job
- `CodFailedEmail` job
- `OrderCancelledEmail` job (parent-cancelled + auto-cancelled)
- Each: render template, send via SMTP, write `email_log`, retry 3x with backoff

---

# Sprint 5 — Pickup + Exchanges + Reports (Weeks 11–12)

## BE-076 — Exchange endpoint (multi-step tx)
**SP:** 4
**Dependencies:** BE-071
**Acceptance:**
- `POST /api/v1/admin/orders/{id}/exchanges` body `{ orderItemId, newVariantId, qty, reason }`
- Flow:
  1. Load order_item (get `old_variant_id`, `unit_price_snapshot`)
  2. Load new variant (get `price_incl_vat`, `stock`)
  3. If `new_stock < qty` → 409
  4. BEGIN TX
  5. UPDATE old variant: `stock = stock + qty`
  6. UPDATE new variant: `stock = stock - qty`
  7. price_delta = (new_price - old_price) * qty
  8. UPDATE order: `total = total + price_delta`
  9. INSERT `exchange` row
  10. INSERT `audit_log`
  11. COMMIT
- Returns: `{ exchangeId, priceDelta, newTotal, cashSettlement }`
- cashSettlement: `parent_pays_<delta>` | `refund_parent_<|delta|>` | `even`

## BE-077 — Exchange table migration
**SP:** 1
**Dependencies:** BE-019
**Acceptance:**
- Entity: `exchange` (id, order_id, order_item_id, old_variant_id, new_variant_id, qty, price_delta, reason, created_at)
- FKs to order, order_item, variant (old + new)
- Index on `exchange(order_id)`

## BE-080 — Reports endpoints
**SP:** 5
**Dependencies:** BE-071
**Acceptance:**
- `GET /api/v1/admin/reports/sales?from=&to=&group_by=day|week|month&school_id=&channel=`
  - Returns: `{ rows: [{ period, orders_count, revenue }], totals: { orders_count, revenue } }`
- `GET /api/v1/admin/reports/inventory`
  - Returns: `{ variants: [{ id, product_name, size, stock, threshold, status }], low_stock_count }`
- `GET /api/v1/admin/reports/notify-me`
  - Returns: `{ variants: [{ id, product_name, size, request_count }] }` ordered by request_count DESC
- All admin-auth required
- p95 < 1s for each

---

# Sprint 6 — Notify-Me + Polish + Harden (Weeks 13–14)

## BE-031 — Notify-me subscription endpoint
**SP:** 2
**Dependencies:** BE-039
**Acceptance:**
- `POST /api/v1/variants/{id}/notify-me` body `{ email }`
- Checks variant is OOS (stock = 0); if in stock → 409
- Dedup: check `pending_alert` where `(variant_id, email_hash) AND notified = false`; if exists → 409
- INSERT `pending_alert` (email_hash = SHA-256(email), notified = false)
- Audit log
- Returns 201

## BE-032 — Notify-me restock email job
**SP:** 3
**Dependencies:** BE-060, BE-022
**Acceptance:**
- `SendNotifyMeEmailsJob(variantId)` Hangfire job
- Query: `SELECT * FROM pending_alert WHERE variant_id = @id AND notified = false`
- For each: send "back in stock" email, set `notified = true`, `notified_at = NOW`
- Write `email_log` per send
- Retries via Hangfire (3 attempts)
- Idempotent: if job re-runs, `notified = true` rows are skipped

## BE-082 — Cancel order endpoint (parent, by token)
**SP:** 3
**Dependencies:** BE-021
**Acceptance:**
- `POST /api/v1/orders/by-token/{token}/cancel` body `{ reason? }`
- Hash token, lookup order
- Guard: state IN (`placed`, `ready_to_ship`, `ready_for_pickup`); else 409
- BEGIN TX → UPDATE state = `cancelled`, `cancelled_at = NOW` → refund stock per item → INSERT audit_log → COMMIT
- Enqueue `OrderCancelledEmail`
- Returns 200 `{ state: "cancelled" }`

## BE-095 — Rate limiting
**SP:** 2
**Dependencies:** BE-005
**Acceptance:**
- Public endpoints: 60 req/min per IP (sliding window)
- `POST /api/v1/orders`: 5 req/s per IP (stricter on checkout)
- Admin endpoints: 30 req/min per admin
- 429 response with `Retry-After` header
- In-memory store (no Redis for v1)

## BE-096 — Input validation hardening
**SP:** 2
**Dependencies:** all prior endpoints
**Acceptance:**
- FluentValidation on every request body
- Phone: Egyptian format validation (`^01[0-9]{9}$`)
- Email: RFC-compliant
- Strings: max length, no SQL metacharacters (EF Core parameterizes, but defense-in-depth)
- Arabic text fields: allow Arabic + Latin + digits + basic punctuation
- 422 on any validation failure with field-level errors

## BE-097 — Idempotency key store
**SP:** 2
**Dependencies:** BE-002
**Acceptance:**
- `Idempotency-Key` header on `POST /orders` and `PUT /variants/{id}/stock`
- Store: `idempotency_key` table (key, request_hash, response_status, response_body, created_at, expires_at)
- On hit: return cached response
- On miss: process, cache response, return
- TTL: 24h

---

# Sprint 7 — Deployment & Launch Prep (Weeks 15–16)

## BE-100 — MonsterASP production deploy
**SP:** 3
**Dependencies:** all prior tasks
**Acceptance:**
- `dotnet publish` artifact configured
- Connection string → production SQL Server tier
- HTTPS enforced by MonsterASP
- `/uploads` directory configured + write permissions
- Hangfire dashboard locked to admin IP
- CORS locked to Vercel production origin

## BE-101 — Bosta production credentials
**SP:** 1
**Dependencies:** BE-100
**Acceptance:**
- Sandbox → production API key cutover
- Webhook URL registered with Bosta production: `https://api.<domain>/api/v1/webhooks/bosta`
- HMAC shared secret set in env
- Test: create a real shipment, verify webhook fires

## BE-102 — Gmail SMTP SPF/DKIM/DMARC
**SP:** 1
**Dependencies:** BE-100
**Acceptance:**
- Sender domain DNS records: SPF, DKIM, DMARC
- Test email delivers to Gmail + Outlook (not spam)
- App password configured in env

## BE-103 — DB backup verification
**SP:** 1
**Dependencies:** BE-100
**Acceptance:**
- Nightly full backup job (MonsterASP scheduler or SQL Agent)
- Test restore on a secondary volume
- Document restore procedure in `docs/RUNBOOK.md`

## BE-104 — Data seeding (production)
**SP:** 2
**Dependencies:** BE-100
**Acceptance:**
- Script to import real schools, grade-stages, item-types, products, variants, images
- Admin account created with owner email + temp password
- First-login recovery code generated and handed to owner

---

## Summary

| Sprint | Tasks | Total SP |
|--------|-------|----------|
| 0 | BE-001 to BE-007 | 13 |
| 1 | BE-010 to BE-052 | 22 |
| 2 | BE-019 to BE-022 | 13 |
| 3 | BE-039 to BE-090 | 21 |
| 4 | BE-069 to BE-075a | 23 |
| 5 | BE-076 to BE-080 | 10 |
| 6 | BE-031 to BE-097 | 13 |
| 7 | BE-100 to BE-104 | 8 |
| **Total** | **52 tasks** | **123 SP** |
