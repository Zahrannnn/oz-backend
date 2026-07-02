# System Design Document (SDD)

# School Uniform Store Platform

**Version:** 1.0
**Author:** Mohamed Zahran
**Status:** Draft (aligned with BRD v1.2 + PRD v1.2)
**Date:** July 2026
**Source:** Product-owner interview Q1–Q31, BRD v1.2, PRD v1.2

---

# Table of Contents

1. Overview
2. Step 1 — Requirements & Scope
3. Step 2 — High-Level Design
4. Step 3 — Deep-Dive on Critical Components
5. Step 4 — Tradeoffs, Bottlenecks, Future
6. Reliability & Operations
7. Quick Diagnostic Score

---

# 1. Overview

Single-city Egyptian school uniform retailer going online. Ship/deliver-first retail flow (Bosta courier + in-store pickup), guest checkout (no parent accounts), one owner / one admin, Arabic-only RTL storefront, in-store exchange only, MS SQL Server + .NET 10 backend on MonsterASP, Next.js 16 frontend on Vercel. Target v1 launch replaces paper-based in-store operations with a digital storefront and admin portal.

---

# 2. Step 1 — Requirements & Scope

## 2.1 Functional Requirements (from PRD)

| # | Capability |
|---|------------|
| F1 | Browse schools (list + name search) |
| F2 | Browse products (filter: school, grade-stage, gender, item-type) |
| F3 | Product details (gallery, size selection, stock status, add-to-cart, add-full-set, notify-me) |
| F4 | Shopping cart (localStorage; persists to DB only at checkout) |
| F5 | Checkout (guest: name + phone + email + address + delivery method; COD) |
| F6 | Order tracking (per-order token URL; timeline; current status) |
| F7 | Notify-me (email capture on OOS; restock triggers email) |
| F8 | Admin auth (email + password + recovery code) |
| F9 | Admin CRUD: schools, grade-stages, item-types, products, variants, product images |
| F10 | Admin inventory: manual stock edit + diff-based notify-me trigger + low-stock widget |
| F11 | Admin orders: state transitions, cancellation, Bosta pickup booking |
| F12 | Admin exchanges: in-store, partial, stock moves, total recompute |
| F13 | Admin reports: sales summary, inventory status, order list, notify-me demand |
| F14 | Scheduled job: 5-day auto-cancel + restock for pre-handoff orders |

## 2.2 Non-Functional Requirements

| NFR | Target | Source |
|-----|--------|--------|
| Page load (catalog) | < 2 s | PRD NFR Performance |
| Checkout | < 3 s | PRD NFR Performance |
| Admin dashboard | < 2 s | PRD NFR Performance |
| Availability | 99.5 % | PRD NFR Availability (~43 h downtime / year) |
| Scale | 10 K products, 100 K variants | PRD NFR Scalability (locked) |
| Latency budget (frontend → backend RTT) | < 200 ms (same continent) | Derived |
| Browser support | Modern evergreen, mobile-first | Inferred |
| Accessibility | RTL Arabic, keyboard nav, screen reader | PRD NFR Accessibility |
| Security | HTTPS, JWT (httpOnly cookie), security headers, rate limiting, audit log | PRD NFR Security |

## 2.3 Scale & Capacity Estimation

### Customers & orders

- Catalog at steady state: ~30 schools × ~3 grade-stages × ~10 item-types × 2 genders = ~1,800 products, ~10,800 variants. PRD ceiling (10K / 100K) gives ~5–10× headroom.
- Back-to-school peak (Aug–Sep, 6 weeks): ~200 orders / day, ~4 hours of active traffic (16:00–20:00 local).
- Off-peak (Oct–Jul): ~10–30 orders / day.
- Annual volume: ~10 K orders / year.
- Notify-me requests: ~5 K / year (OOS seasons for popular sizes).

### QPS

| Path | Avg QPS | Peak QPS | Notes |
|------|---------|----------|-------|
| Order placement (write) | 0.0003 | 0.014 | 200 orders / 4 h peak |
| Catalog page reads (browser) | 0.7 | 5 | 200 parents × 50 views |
| Admin backend | < 0.01 | < 0.1 | One owner |
| Order status page (token) | 0.05 | 0.5 | Parent checks a few times |
| Notify-me (restock emails) | bursty | bursty | Fires only on restock events |

Peak total ≈ **6 QPS** combined. Comfortably under the 1-instance ceiling of any reasonable backend.

### Storage (Year 1)

| Asset | Size |
|-------|------|
| DB catalog metadata (products, variants, schools, item-types, grade-stages) | ~25 MB |
| DB orders (10 K × 2 KB including items) | ~20 MB |
| DB notify-me (5 K × 200 B) | ~1 MB |
| DB audit log (~50 actions / day × 1 KB × 365) | ~18 MB / year |
| Product images (10 K × 3 × 200 KB, local FS) | ~6 GB |
| Email log (SMTP send log) | ~5 MB / year |
| **DB total** | **~70 MB / year** |
| **Filesystem total** | **~6 GB** |

### Bandwidth

Peak ≈ 5 page-views / s × ~200 KB avg page weight = **1 MB / s ≈ 8 Mbps**. Well within MonsterASP and Vercel free tiers.

### Latency budget (Vercel → MonsterASP RTT)

- Same continent (Cairo → MonsterASP Egypt) ≈ 30–80 ms.
- Cross-request overhead (auth + serialization) ≈ 20 ms.
- Effective p95 budget per dynamic API call: **< 250 ms** (target).

---

# 3. Step 2 — High-Level Design

## 3.1 Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│ Parent / Admin Browser                                           │
│  - Arabic RTL, Tailwind, modern evergreen                       │
│  - localStorage cart (guests)                                    │
└────────────────┬─────────────────────────────────────────────────┘
                 │ HTTPS, JSON
                 ↓
┌──────────────────────────────────────────────────────────────────┐
│ Vercel — Next.js 16 (React 19)                                   │
│  - Catalog (schools / grades / products): SSR + ISR              │
│  - Dynamic (cart / checkout / order token / admin): client-side  │
│  - CORS locked to Vercel origin                                  │
└────────────────┬─────────────────────────────────────────────────┘
                 │ HTTPS + JWT (admin) / public REST (guest)
                 ↓
┌──────────────────────────────────────────────────────────────────┐
│ MonsterASP — .NET 10 Web API                                     │
│  - Controllers (REST), EF Core (MSSQL), Hangfire (scheduler)     │
│  - Bosta client (booking, webhook)                               │
│  - SMTP client (Gmail)                                           │
│  - Static file server for /uploads (product images)              │
│  - Audit log writer                                              │
└──┬──────────────────┬──────────────────┬─────────────────┬────────┘
   │                  │                  │                 │
   ↓                  ↓                  ↓                 ↓
┌──────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ MS SQL   │  │ Hangfire     │  │ Bosta API    │  │ Gmail SMTP   │
│ Server   │  │ job store    │  │ (external)   │  │ (external)   │
│ (catalog,│  │ (MSSQL-backed│  │ booking +    │  │ order-updates│
│ orders,  │  │ recurring)   │  │ tracking +   │  │ + notify-me  │
│ stock,   │  │              │  │ webhooks     │  │ alerts       │
│ notify,  │  │              │  │              │  │              │
│ audit)   │  │              │  │              │  │              │
└──────────┘  └──────────────┘  └──────────────┘  └──────────────┘
```

## 3.2 Data flow — happy path (parent places a delivery order)

1. Parent opens `/` (Vercel SSR + ISR, 1 h revalidate). Catalog served from Vercel edge cache.
2. Parent browses school → grade-stage → product grid. Each page is ISR-revalidated; product detail uses ISR.
3. Parent adds items to cart in `localStorage`; clicks "Checkout".
4. Browser POSTs `/api/orders` with cart + customer fields + delivery choice (client-side fetch).
5. .NET controller starts DB transaction:
   1. Lock variant rows (`SELECT … WITH (UPDLOCK, ROWLOCK)`).
   2. Check `stock >= qty` for each line. If any fails → `409 Conflict`, abort.
   3. INSERT `orders` row (state = `placed`, generate token, capture customer fields, totals).
   4. INSERT `order_items` rows.
   5. UPDATE `variant.stock -= qty` for each line.
   6. INSERT `audit_log` row.
   7. COMMIT.
6. Enqueue `OrderConfirmationEmail` Hangfire job.
7. Return `{ orderId, token, tokenUrl, total }` to client.
8. Client shows confirmation page with the token URL and "save this link to track your order".

## 3.3 API surface (representative)

### Public (no auth)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/schools` | List schools (cached client-side; MiniSearch indexed) |
| GET | `/api/schools/{id}/products` | Product grid for a (school, grade-stage) pair |
| GET | `/api/products/{id}` | Product detail (gallery, variants, stock, set flag) |
| POST | `/api/products/{id}/notify-me` | Capture email on OOS product |
| POST | `/api/orders` | Place order (cart + customer fields) |
| GET | `/api/orders/by-token/{token}` | Parent order status (timeline) |
| POST | `/api/webhooks/bosta` | Bosta tracking webhooks (HMAC-verified) |

### Admin (JWT in httpOnly cookie)

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/admin/auth/login` | Email + password |
| GET | `/api/admin/dashboard` | KPI tiles |
| CRUD | `/api/admin/{schools,grade-stages,item-types,products,variants,product-images}` | Catalog management |
| GET/POST | `/api/admin/variants/{id}/stock` | Manual stock edit (diff-based triggers) |
| GET | `/api/admin/orders` | Filterable list |
| POST | `/api/admin/orders/{id}/transition` | State transition (whitelisted) |
| POST | `/api/admin/orders/{id}/cancel` | Cancel order (whitelisted states) |
| POST | `/api/admin/orders/{id}/bosta-pickup` | Book Bosta pickup |
| POST | `/api/admin/orders/{id}/exchange` | Log exchange (in-store) |
| GET | `/api/admin/reports/{sales,inventory,orders,notify-me}` | v1 reports |
| GET | `/api/admin/audit-log` | Audit log (filterable, CSV export) |

## 3.4 Data model (high level)

Tables (MSSQL):

- `school` (id, name, type, is_active, created_at)
- `grade_stage` (id, school_id, name) — unique per (school_id, name); some schools have one
- `item_type` (id, name) — shared across schools
- `product` (id, school_id, grade_stage_id, item_type_id, gender, color, is_in_set, is_archived, created_at)
- `variant` (id, product_id, size_label, price_incl_vat, stock, low_stock_threshold, created_at)
- `product_image` (id, product_id, url, sort_order)
- `order` (id, token, state, channel, customer_name, customer_phone, customer_email, address_city, address_line, delivery_fee, total, bosta_tracking_id, created_at, state_changed_at, cancelled_at, delivered_at, picked_up_at)
- `order_item` (id, order_id, variant_id, qty, unit_price_snapshot, line_total_snapshot)
- `exchange` (id, order_id, order_item_id, old_variant_id, new_variant_id, qty, price_delta, reason, created_at)
- `notify_request` (id, variant_id, email, created_at, fulfilled_at) — unique on (variant_id, email) where `fulfilled_at IS NULL`
- `admin` (id, email, password_hash, recovery_code_hash, created_at)
- `audit_log` (id, actor_id, action, entity_type, entity_id, before_json, after_json, reason, created_at)
- `email_log` (id, order_id, template, to_email, status, sent_at, error)
- `scheduled_job` — managed by Hangfire (auto-cancel recurring)

Foreign keys: `product.school_id`, `product.grade_stage_id`, `product.item_type_id`; `variant.product_id`; `order_item.order_id`, `order_item.variant_id`; `exchange.order_id`, `exchange.order_item_id`, `exchange.old_variant_id`, `exchange.new_variant_id`; `notify_request.variant_id`.

---

# 4. Step 3 — Deep-Dive on Critical Components

## 4.1 Order placement (atomicity & oversell prevention)

**Risk:** two parents order the last size simultaneously. Without locks, both succeed and one ships an empty box.

**Design:**
- `PlaceOrder` runs inside a single SQL transaction, default `READ COMMITTED` isolation.
- For each variant in the cart, the controller issues a `SELECT stock FROM variant WITH (UPDLOCK, ROWLOCK) WHERE id = @id`. The row is locked for the duration of the transaction.
- Application checks `stock >= qty`. If any line fails, transaction rolls back, controller returns `409 Conflict` with the offending variant id.
- On success: insert order, insert order_items, `UPDATE variant SET stock = stock - @qty WHERE id = @id`, insert audit_log row, commit.
- Hangfire enqueues confirmation email after commit (best-effort, not in the same transaction).

**Tradeoff:** row-level lock serializes concurrent orders against the same variant only — different variants run in parallel. At 0.014 orders / s peak, lock contention is negligible. If we ever batch-order (v2), revisit isolation level.

**Failure handling:**
- DB error mid-transaction → rollback, return 500, audit log entry.
- Email send failure post-commit → logged in `email_log`; Hangfire retries with backoff (3 attempts); order itself is already placed (correctness > notification).

## 4.2 Order state machine & 5-day auto-cancel

**States:**
- Delivery: `placed → ready_to_ship → handed_to_courier → in_transit → delivered → closed_success`
- Delivery failure: `in_transit → cod_failed → returned_to_store → closed_failed`
- Pickup: `ready_to_ship → ready_for_pickup → picked_up → closed_success`
- Cancellation: from `placed` / `ready_to_ship` (delivery) or `placed` / `ready_to_ship` / `ready_for_pickup` (pickup) → `cancelled`

**Guards:** the `TransitionOrder` controller holds a static whitelist of `{fromState, toState}` pairs. Any other pair returns `409`. Admins and parents see only the actions allowed for the current state.

**5-day auto-cancel:**
- Hangfire recurring job, daily at 03:00 server time. (In-process on the .NET app; MSSQL-backed job store.)
- Query: `SELECT id FROM orders WHERE state IN ('placed','ready_to_ship','ready_for_pickup') AND state_changed_at < DATEADD(day, -5, GETUTCDATE())`.
- For each: open transaction → transition to `cancelled` → refund stock per line → write audit log row → enqueue `OrderCancelledEmail`.
- Job is idempotent: re-running on the same order is safe (state has already changed; the query returns nothing).

**Why pre-handoff only:** `handed_to_courier` / `in_transit` are in Bosta's hands — cancelling means a returned-to-store package and a Bosta dispute. Not in scope for the auto-cancel.

## 4.3 Notify-me diff trigger on manual stock edit

**Endpoint:** `POST /api/admin/variants/{id}/stock` body `{ newStock, reason }`.

**Flow:**
1. Open transaction. `SELECT stock FROM variant WITH (UPDLOCK, ROWLOCK) WHERE id = @id`. Capture `oldStock`.
2. `UPDATE variant SET stock = @newStock, low_stock_threshold = COALESCE(@threshold, low_stock_threshold)`.
3. Audit log row: `{action: "stock_edit", before: {stock: oldStock}, after: {stock: newStock}}`.
4. Commit.
5. **Outside** the transaction, run two checks:
   - If `oldStock == 0 AND newStock > 0`: enqueue `NotifyRestockEmailJob(variantId)` Hangfire job. The job queries `notify_request` rows where `variant_id = @id AND fulfilled_at IS NULL`, sends each an email, and sets `fulfilled_at = NOW()` to prevent re-fire.
   - If `newStock < low_stock_threshold`: upsert a row into `low_stock_cache` (read by the admin dashboard widget on every load; no email).
6. Return updated variant to admin.

**Why outside the transaction:** the email sends are best-effort and should not block the stock write or hold the lock. Hangfire retries handle transient SMTP failures.

**Edge case:** if the admin types the same number twice (no diff), no notify-me fires. Only `0 → positive` transition triggers. Acceptable per the locked decision.

## 4.4 Bosta integration (per-order booking + webhooks)

**Pickup booking (admin action):**
- Admin opens order, reviews, clicks "Book Bosta pickup".
- Controller calls `BostaClient.CreateShipment(orderId, customer, address, codAmount)` (HTTP POST to Bosta, secured by API key in header).
- On 2xx: store `bosta_tracking_id`, transition order `ready_to_ship → handed_to_courier`, set `state_changed_at = NOW()`, audit log, enqueue `OrderShippedEmail` (with token URL). Return 200 to admin.
- On non-2xx / timeout: return 502 to admin, log error, do not change state. Admin retries; no batch UI per Q28.

**Webhook (Bosta → us):**
- POST `/api/webhooks/bosta` with JSON body. HMAC-SHA256 header; verify against shared secret in env. Reject 401 on mismatch.
- Parse `tracking_id` and `status`. Map:
  - `in_transit` → no state change (already at `in_transit` after pickup). If still at `handed_to_courier`, advance to `in_transit`.
  - `delivered` → `in_transit → delivered`, set `delivered_at = NOW()`, enqueue `OrderDeliveredEmail`, transition to `closed_success` after a short delay (so admin can re-print invoice if needed). For v1: transition immediately.
  - `cod_failed` → `in_transit → cod_failed`, enqueue `CodFailedEmail`.
  - `returned_to_store` → `cod_failed → returned_to_store → closed_failed`, refund stock (we got the item back), audit log.
- Webhook handler is idempotent: re-delivery of the same status is a no-op.

**Retry policy:** Bosta is responsible for retrying their own webhook deliveries. We do not implement an outbound retry; the inbound handler is idempotent.

## 4.5 Hybrid fetching (Vercel SSR+ISR for catalog, client for dynamic)

**Why hybrid:** catalog is SEO-critical (parents Google school names) and rarely changes; cart / checkout / admin are dynamic and per-user.

**Catalog (SSR + ISR):**
- Pages: `/`, `/schools/[id]`, `/schools/[id]/[gradeStage]`, `/products/[id]`.
- Render strategy: `revalidate = 3600` (1 hour) as default. Faster revalidation is allowed via on-demand revalidation: a Hangfire job on the backend fires `POST /api/revalidate` on Vercel after a successful admin product CRUD, with the affected paths as tags. This gives near-instant updates without polling.
- Cart / checkout / order tracking: client components only, fetch from `.NET` directly with `fetch` (no Next.js API routes — confirmed in interview).

**CORS:** ASP.NET Core middleware allows the Vercel deployment origin only. Admin JWT in `httpOnly; Secure; SameSite=Lax` cookie.

**Why no Next.js API routes:** keeps the .NET backend as the single source of truth. Avoids two API surfaces, no CORS-on-CORS, no auth-token forwarding complexity.

## 4.6 Admin audit log

**Writer:** every state-changing action (order transition, stock edit, exchange log, CRUD on catalog entities, admin login success/failure) writes an `audit_log` row. Read-only views skip logging.

**Reader:** admin endpoint `GET /api/admin/audit-log?actor=&action=&from=&to=&entity_type=`. CSV export. Retention: indefinite for v1 (storage trivial at ~18 MB / year).

**Indexing:** `(created_at DESC)` and `(actor_id, created_at DESC)` for filter patterns.

## 4.7 Search — MiniSearch client-side + MSSQL `LIKE` server-side

**Frontend (MiniSearch):**
- On initial load, `/api/schools` returns the full ~30-school list. The Next.js page instantiates MiniSearch and indexes the list in memory.
- Search input queries MiniSearch with fuzzy + prefix matching. Result is instant, zero network roundtrip.
- Field weight: name (1.0), name prefix boost (1.5).

**Backend (MSSQL `LIKE`):**
- `GET /api/schools?q=…` performs `WHERE name LIKE '%' + @q + '%' COLLATE Arabic_CI_AS` for any server-side filtering (admin selectors, future product search).
- Index: `CREATE NONCLUSTERED INDEX IX_school_name ON school(name)` for the v1 catalog size; full-text index deferred until name search becomes a hot path.

---

# 5. Step 4 — Tradeoffs, Bottlenecks, Future

## 5.1 Tradeoffs (named explicitly)

| Choice | Cost | Benefit |
|--------|------|---------|
| MSSQL single instance (MonsterASP) | Single point of failure for DB | Cheap, simple ops, fits scale |
| .NET 10 + Next.js 16 split | Extra HTTP hop ~30–80 ms; no shared types; two deploys | Clean separation; each stack uses its native idiom |
| localStorage cart | Cart lost on device clear; no cross-device resume | Zero backend complexity, no session store |
| Gmail SMTP | 500/day free, 2K/day Workspace | Zero vendor setup; product-owner accepted the cap |
| Hangfire in-process | App restart loses in-flight jobs (MSSQL-backed store recovers schedule) | No extra worker process to manage |
| Bosta per-order booking (no batch) | Aug-Sep click-fatigue for admin | Per the locked Q28 decision |
| local image storage on MonsterASP | No CDN, slow for far-away clients, manual backup | No extra vendor; matches the "lean launch" interview stance |
| Single JWT, no refresh | 8h cookie expiry mid-session is possible (mitigated by sliding renewal) | One token, no rotation complexity |
| `is_in_set` boolean (not a `uniform_set` table) | One set per (school + grade + gender) | Simpler admin UX, less code |

## 5.2 Bottlenecks to watch

1. **DB write throughput under Aug–Sep peak.** Estimated peak ≈ 0.014 orders / s. MSSQL shared-tier handles 100+ TPS trivially. No action needed.
2. **Image bandwidth on slow connections.** 6 GB local FS, no CDN. Parents on 3G will see slow product detail. Mitigation when warranted: Cloudflare free tier in front of `/uploads`. Out of v1 scope.
3. **Email daily cap (Gmail free = 500).** Back-to-school month could hit this. Mitigation: upgrade to Workspace (2K/day) or move to Mailgun. Out of v1.
4. **ISR revalidation latency after admin product CRUD.** With 1 h default revalidate, a new product may not show for up to 1 h unless on-demand revalidation fires. Mitigation: on-demand revalidation after every successful admin write (see 4.5).
5. **Hangfire single scheduler.** A long-running Hangfire crash delays the 5-day auto-cancel. Mitigation: Hangfire retry on missed triggers; the auto-cancel is idempotent so it self-heals on next run.

## 5.3 Future (v2+)

- POS integration (channel = pos added to `orders`)
- Online payments (Paymob via `PaymentProvider` interface already reserved)
- Multi-store / multi-city (add `store_id` to relevant tables)
- Multiple delivery providers (extend `ShippingProvider`)
- Mobile apps
- Loyalty, coupons
- CDN in front of `/uploads`
- Search de-normalization on `product.name` if school list grows
- Read replica if admin reports become slow at scale (unlikely at v1)

---

# 6. Reliability & Operations

## 6.1 Health checks

- **Liveness:** `/healthz` returns 200 if the .NET process is alive (no DB call). Used by MonsterASP / load balancer.
- **Readiness:** `/readyz` returns 200 only if (a) MSSQL reachable, (b) Hangfire schema initialized, (c) Bosta API key configured. Used to gate traffic after a cold start.

## 6.2 Monitoring & alerting

**Metrics (Prometheus-style, scraped from `/metrics`):**
- HTTP request rate, error rate (5xx), p50 / p95 / p99 latency per endpoint.
- DB connection pool usage, query p95.
- Hangfire queue depth, job success / failure count.
- Outbound: Bosta call success rate + p95, SMTP send success rate + p95.
- Business: orders placed / hour, low-stock variant count, notify-me backlog.

**Logs:** structured JSON to stdout. Aggregated by MonsterASP's log sink (TBD; ship to ELK / Loki later if volume justifies).

**Traces:** OpenTelemetry on the .NET side; Vercel has built-in tracing for the frontend. Span IDs propagate through `traceparent` header.

**Alerts (PagerDuty or email):**
- Error rate > 1% sustained 5 min → page.
- DB unreachable 30 s → page.
- Hangfire auto-cancel job missed its daily run → page.
- Bosta webhook signature failures > 5 / min → security alert.
- Outbound SMTP success rate < 95% sustained 10 min → page.
- Free disk on `/uploads` < 10% → warn.

## 6.3 Deployment strategy

- **Frontend (Vercel):** git-push to `main` triggers a Vercel preview deployment. Merge to `main` → production. Environment variables per branch.
- **Backend (MonsterASP):** git tag `vX.Y.Z` triggers a CI pipeline that:
  1. Builds the .NET project (`dotnet publish`).
  2. Runs migrations against the staging DB.
  3. Deploys the artifact to a staging slot on MonsterASP.
  4. Runs smoke tests (health check + a synthetic order in dry-run mode).
  5. Blue-green cutover via MonsterASP's slot swap. Old slot kept warm for 30 min as instant rollback.
- **Database migrations:** EF Core migrations applied by the CI pipeline in the deploy step. Migrations are forward-only in v1 (no rollback migrations); rollback = restore DB from the last backup.

## 6.4 Disaster recovery (RPO / RTO)

| Asset | RPO | RTO | Mechanism |
|-------|-----|-----|-----------|
| MSSQL database | 24 h | 1 h | Nightly full backup to a second MonsterASP volume. Restore procedure documented and tested quarterly. |
| Product images (`/uploads`) | 24 h | 2 h | Same nightly backup (rsync of the `/uploads` directory). |
| Hangfire jobs | 0 (MSSQL-backed) | 0 | Job state lives in the DB; survives app restart. |
| Admin session (JWT) | n/a | 0 | Stateless; re-login if expired. |

**Single-region:** v1 is single-region (MonsterASP Egypt). Multi-region is a v2+ concern; the architecture does not block it (stateless app, DB has standard backup/restore).

## 6.5 Scaling strategy (DB)

- **v1 (current):** single MSSQL instance, vertical scaling, EF Core connection pool.
- **Trigger to add read replicas:** admin dashboard p95 > 1 s or reports p95 > 3 s under load. Add 1 read replica, route reports and dashboard reads to it.
- **Trigger to shard:** > 1 M variants, or peak write QPS > 50. Shard key candidate: `school_id` (each school is naturally independent). Cross-shard reporting via per-shard nightly export + central aggregator.

---

# 7. Quick Diagnostic Score

| # | Question | Pass? | Evidence / Fix |
|---|----------|-------|----------------|
| 1 | Are functional and non-functional requirements listed? | ✅ | §2.1, §2.2 — every requirement traced to PRD/BRD source |
| 2 | Is there a QPS and storage estimate? | ✅ | §2.3 — QPS table, storage table, bandwidth |
| 3 | Is every component redundant? | ❌ | Single MSSQL, single .NET instance, single MonsterASP host, single Vercel project. **Fix when scale requires:** add a second MonsterASP VM behind a TCP load balancer (stateless app, MSSQL is the only stateful piece). MSSQL stays single until v2. Vercel already has multi-AZ redundancy for the frontend. |
| 4 | Is the database scaling strategy defined? | ✅ | §6.5 — vertical → read replicas → sharding with explicit triggers and shard key |
| 5 | Is there a cache for read-heavy paths? | ⚠️ | ISR serves the catalog read path (effectively a CDN cache with on-demand revalidation). No Redis layer. **Fix when needed:** add Redis cache-aside in front of `/api/products/{id}` and admin dashboard queries. Out of v1. |
| 6 | Are async paths using queues? | ✅ | Hangfire for order-confirmation emails, restock notify-me, 5-day auto-cancel, Bosta retry (idempotent). |
| 7 | Is there a monitoring and alerting plan? | ✅ | §6.2 — metrics, logs, traces, alerts with thresholds |
| 8 | Is the deployment strategy defined? | ✅ | §6.3 — Vercel git-push for frontend, blue-green CI for backend, EF Core migrations in pipeline |

**Score:** round(6.5 / 8 × 10) = **8 / 10**

**Failing / partial rows and the fix for each:**
- Row 3 (redundancy): acceptable at v1 scale (peak 6 QPS); add second .NET instance + TCP LB when sustained QPS > 100.
- Row 5 (Redis): acceptable because ISR + on-demand revalidation already cache the catalog. Add Redis when admin dashboard p95 > 1 s or product-detail endpoint p95 > 200 ms under load.
