# Database Design Document

# School Uniform Store Platform

**Version:** 1.0
**Author:** Mohamed Zahran
**Status:** Draft (aligned with BRD v1.2 + PRD v1.2 + SDD v1.0)
**Date:** July 2026
**Engine:** Microsoft SQL Server (MonsterASP-hosted)

---

# Table of Contents

1. Design Principles
2. Conventions
3. Schema Overview (ERD)
4. Tables
5. Indexes
6. Constraints
7. Triggers vs Application Logic
8. Data Migration & Seeding
9. Backup & Recovery
10. Scaling Strategy

---

# 1. Design Principles

- **Relational first.** Catalog is highly structured (school → grade → item → product → variant). MSSQL fits.
- **Single schema, single database.** v1 is single-store, single-city. No multi-tenant splitting yet.
- **Application owns business logic.** DB enforces referential integrity, uniqueness, and not-null. State machine guards, audit logging, and stock arithmetic live in the .NET layer (testable, observable).
- **Soft delete for catalog.** Products and schools use `is_archived` so historical orders keep resolving names. Hard delete breaks order history.
- **Snapshot prices on order.** `order_item.unit_price_snapshot` freezes the price paid. Later admin price edits do not mutate historical orders.
- **Token storage.** `orders.tracking_token_hash` stores a SHA-256 hash. The plaintext token lives only in the URL and emails. Lookup compares hash.
- **All timestamps UTC.** `DATETIME2(3)` columns, named `*_at`. Application converts to Egypt local time for display.
- **Money in EGP, VAT-inclusive.** `DECIMAL(10,2)` for prices and totals. No separate tax column in v1 (VAT is embedded in the displayed price per locked Q17).

---

# 2. Conventions

| Convention | Choice |
|------------|--------|
| Primary keys | `BIGINT IDENTITY(1,1)` for all tables except `admin` (uses `UNIQUEIDENTIFIER` for forward-compat with multi-admin) |
| Table names | Singular, snake_case (`school`, `order_item`, `notify_request`) |
| Column names | snake_case (`created_at`, `is_archived`, `tracking_token_hash`) |
| Foreign keys | Named `fk_<table>_<ref>`, indexed |
| Booleans | `BIT` (0/1) |
| Strings | `NVARCHAR(n)` for Arabic / freeform text; `VARCHAR(n)` for codes / emails (ASCII) |
| Timestamps | `DATETIME2(3)` UTC |
| Money | `DECIMAL(10,2)` |
| Enums | Stored as `TINYINT` with a CHECK constraint; decoded in application |
| Soft delete | `is_archived BIT NOT NULL DEFAULT 0` |
| Audit | All state-changing writes go through the application layer which inserts into `audit_log` |

---

# 3. Schema Overview (ERD)

```
school 1───* grade_stage
school 1───* product
grade_stage 1───* product
item_type 1───* product
product 1───* variant
product 1───* product_image
variant 1───* order_item
variant 1───* notify_request
variant 1───* exchange (as old_variant_id and new_variant_id)
order 1───* order_item
order 1───* exchange
order_item 1───* exchange
admin 1───* audit_log (actor)
order 1───* email_log
```

Aggregates:
- **Catalog aggregate:** `school` is the root. Grade-stages, products, variants, images live under it (via product → school).
- **Order aggregate:** `order` is the root. `order_item`, `exchange`, `email_log` reference it.
- **Notify-me aggregate:** `notify_request` stands alone, keyed by `(variant_id, email)`.

---

# 4. Tables

## 4.1 `school`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| name | NVARCHAR(200) | NOT NULL |  | Freeform Arabic / English per admin |
| type | TINYINT | NOT NULL |  | CHECK in (1=Governmental/حكومي, 2=Experimental/تجريبي, 3=Arabic/عربي, 4=Language/لغات, 5=International/دولي, 6=Private/خاص) |
| is_archived | BIT | NOT NULL | 0 | Soft delete |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |
| updated_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `UQ_school_name_active` unique on (`name`) WHERE `is_archived = 0`; `IX_school_type` on (`type`).

---

## 4.2 `grade_stage`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| school_id | BIGINT | NOT NULL |  | FK → school.id |
| name | NVARCHAR(100) | NOT NULL |  | e.g. Ebtda2y / E3dady / Sanawy |
| display_order | INT | NOT NULL | 0 | Sort order on storefront |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `UQ_grade_stage_school_name` unique on (`school_id`, `name`); `IX_grade_stage_school` on (`school_id`).

---

## 4.3 `item_type`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| name | NVARCHAR(100) | NOT NULL |  | Shared across schools (e.g. trousers, pullover) |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `UQ_item_type_name` unique on (`name`).

---

## 4.4 `product`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| school_id | BIGINT | NOT NULL |  | FK → school.id |
| grade_stage_id | BIGINT | NOT NULL |  | FK → grade_stage.id |
| item_type_id | BIGINT | NOT NULL |  | FK → item_type.id |
| gender | TINYINT | NOT NULL |  | CHECK in (1=boys, 2=girls, 3=unisex) |
| color | NVARCHAR(100) | NULL |  | Admin-fixed per (school, grade, item) spec |
| is_in_set | BIT | NOT NULL | 0 | True = part of (school+grade+gender) full set |
| is_archived | BIT | NOT NULL | 0 | Soft delete |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |
| updated_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Constraints:** `UQ_product_spec` unique on (`school_id`, `grade_stage_id`, `item_type_id`, `gender`) WHERE `is_archived = 0` — prevents duplicate specs.

**Indexes:** `IX_product_school_grade` on (`school_id`, `grade_stage_id`); `IX_product_item_type` on (`item_type_id`); `IX_product_set` on (`school_id`, `grade_stage_id`, `gender`) WHERE `is_in_set = 1` — accelerates "add full set" lookup.

---

## 4.5 `variant`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| product_id | BIGINT | NOT NULL |  | FK → product.id |
| size_label | NVARCHAR(50) | NOT NULL |  | Free-text per Q8 |
| price_incl_vat | DECIMAL(10,2) | NOT NULL |  | EGP, VAT-inclusive |
| stock | INT | NOT NULL | 0 | Current count |
| reserved | INT | NOT NULL | 0 | Always 0 in v1 (decrement-on-placed); kept for forward-compat |
| low_stock_threshold | INT | NOT NULL | 5 | Configurable per variant |
| is_archived | BIT | NOT NULL | 0 | Soft delete |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |
| updated_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Constraints:** `UQ_variant_product_size` unique on (`product_id`, `size_label`) WHERE `is_archived = 0`; `CK_variant_stock_nonneg` CHECK (`stock >= 0`); `CK_variant_threshold_nonneg` CHECK (`low_stock_threshold >= 0`).

**Indexes:** `IX_variant_product` on (`product_id`); `IX_variant_low_stock` on (`stock`, `low_stock_threshold`) WHERE `stock < low_stock_threshold AND is_archived = 0` — drives the dashboard widget query.

---

## 4.6 `product_image`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| product_id | BIGINT | NOT NULL |  | FK → product.id |
| url | NVARCHAR(500) | NOT NULL |  | Relative path under `/uploads`, e.g. `/uploads/2026/07/abc.jpg` |
| sort_order | INT | NOT NULL | 0 | Gallery ordering |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `IX_product_image_product` on (`product_id`, `sort_order`).

---

## 4.7 `order`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| tracking_token_hash | BINARY(32) | NOT NULL |  | SHA-256 hash of the URL token |
| state | TINYINT | NOT NULL |  | CHECK in (1=placed, 2=ready_to_ship, 3=handed_to_courier, 4=in_transit, 5=delivered, 6=cod_failed, 7=returned_to_store, 8=ready_for_pickup, 9=picked_up, 10=closed_success, 11=closed_failed, 12=cancelled) |
| channel | TINYINT | NOT NULL |  | CHECK in (1=delivery, 2=pickup) |
| customer_name | NVARCHAR(200) | NOT NULL |  |  |
| customer_phone | VARCHAR(20) | NOT NULL |  | E.164-ish; used for counter lookup |
| customer_email | VARCHAR(200) | NOT NULL |  | Used for order-update emails |
| address_city | NVARCHAR(200) | NOT NULL |  | Single-city v1 |
| address_line | NVARCHAR(500) | NULL |  | NULL for pickup orders |
| delivery_fee | DECIMAL(10,2) | NOT NULL | 0 |  |
| total | DECIMAL(10,2) | NOT NULL |  | Recomputed on exchange (Q22) |
| bosta_tracking_id | VARCHAR(100) | NULL |  | Set after pickup booked |
| state_changed_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() | Drives 5-day auto-cancel |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |
| cancelled_at | DATETIME2(3) | NULL |  |  |
| delivered_at | DATETIME2(3) | NULL |  |  |
| picked_up_at | DATETIME2(3) | NULL |  |  |

**Constraints:** `UQ_order_token_hash` unique on (`tracking_token_hash`); `CK_order_state` CHECK (`state IN (1..12)`); `CK_order_channel` CHECK (`channel IN (1, 2)`); `CK_order_pickup_address` CHECK (`channel = 2 AND address_line IS NULL OR channel = 1`).

**Indexes:** `IX_order_token` on (`tracking_token_hash`); `IX_order_state_changed` on (`state`, `state_changed_at`) WHERE `state IN (1, 2, 8)` — drives the auto-cancel scan; `IX_order_phone` on (`customer_phone`); `IX_order_created` on (`created_at`); `IX_order_state` on (`state`).

---

## 4.8 `order_item`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| order_id | BIGINT | NOT NULL |  | FK → order.id |
| variant_id | BIGINT | NOT NULL |  | FK → variant.id |
| qty | INT | NOT NULL |  | CHECK > 0 |
| unit_price_snapshot | DECIMAL(10,2) | NOT NULL |  | Frozen at order time |
| line_total_snapshot | DECIMAL(10,2) | NOT NULL |  | qty × unit_price_snapshot |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `IX_order_item_order` on (`order_id`); `IX_order_item_variant` on (`variant_id`).

**Note:** rows are never mutated. Exchanges reference the original line via `exchange.order_item_id` (see 4.9).

---

## 4.9 `exchange`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| order_id | BIGINT | NOT NULL |  | FK → order.id |
| order_item_id | BIGINT | NOT NULL |  | FK → order_item.id (the original line) |
| old_variant_id | BIGINT | NOT NULL |  | FK → variant.id (returned) |
| new_variant_id | BIGINT | NOT NULL |  | FK → variant.id (taken) |
| qty | INT | NOT NULL | 1 | CHECK > 0 |
| price_delta | DECIMAL(10,2) | NOT NULL |  | (new_price − old_price) × qty; can be negative |
| reason | NVARCHAR(500) | NULL |  | Free text |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `IX_exchange_order` on (`order_id`); `IX_exchange_order_item` on (`order_item_id`).

**Note:** writing an exchange row is a transaction that also:
1. `UPDATE variant SET stock = stock + @qty WHERE id = @old_variant_id` (return)
2. `UPDATE variant SET stock = stock - @qty WHERE id = @new_variant_id` (take), CHECK `stock >= 0` aborts if OOS
3. `UPDATE order SET total = total + @price_delta WHERE id = @order_id`

The original `order_item` row is preserved (audit trail). Multiple exchanges chain on the same `order_item_id`.

---

## 4.10 `notify_request`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| variant_id | BIGINT | NOT NULL |  | FK → variant.id |
| email | VARCHAR(200) | NOT NULL |  | Parent email |
| fulfilled_at | DATETIME2(3) | NULL |  | NULL = pending; set when restock email sent |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Constraints:** `UQ_notify_pending` unique on (`variant_id`, `email`) WHERE `fulfilled_at IS NULL` — prevents duplicate pending requests for the same parent on the same variant.

**Indexes:** `IX_notify_pending_variant` on (`variant_id`) WHERE `fulfilled_at IS NULL` — drives the restock-triggered email fan-out.

---

## 4.11 `admin`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | UNIQUEIDENTIFIER | NOT NULL | NEWSEQUENTIALID() | PK; forward-compat with multi-admin |
| email | VARCHAR(200) | NOT NULL |  |  |
| password_hash | VARCHAR(200) | NOT NULL |  | bcrypt / argon2 encoded |
| recovery_code_hash | VARCHAR(200) | NULL |  | bcrypt hash of the one-time code shown at first login |
| recovery_code_shown_at | DATETIME2(3) | NULL |  |  |
| is_active | BIT | NOT NULL | 1 |  |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |
| last_login_at | DATETIME2(3) | NULL |  |  |

**Constraints:** `UQ_admin_email` unique on (`email`).

---

## 4.12 `audit_log`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| actor_id | UNIQUEIDENTIFIER | NULL |  | FK → admin.id; NULL for system actions (Hangfire, Bosta webhook) |
| action | VARCHAR(100) | NOT NULL |  | e.g. `order.transition`, `stock.edit`, `product.create`, `admin.login_success` |
| entity_type | VARCHAR(50) | NOT NULL |  | `order`, `variant`, `product`, … |
| entity_id | VARCHAR(50) | NOT NULL |  | String-typed to accept BIGINT or UUID ids |
| before_json | NVARCHAR(MAX) | NULL |  | JSON snapshot before |
| after_json | NVARCHAR(MAX) | NULL |  | JSON snapshot after |
| reason | NVARCHAR(500) | NULL |  | Free text from admin |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `IX_audit_created` on (`created_at` DESC); `IX_audit_actor` on (`actor_id`, `created_at` DESC); `IX_audit_entity` on (`entity_type`, `entity_id`).

---

## 4.13 `email_log`

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| id | BIGINT IDENTITY | NOT NULL |  | PK |
| order_id | BIGINT | NULL |  | FK → order.id; NULL for non-order emails |
| notify_request_id | BIGINT | NULL |  | FK → notify_request.id; NULL for non-notify emails |
| template | VARCHAR(50) | NOT NULL |  | `order_confirmation`, `order_shipped`, `order_delivered`, `cod_failed`, `order_cancelled`, `notify_restock` |
| to_email | VARCHAR(200) | NOT NULL |  |  |
| status | TINYINT | NOT NULL |  | CHECK in (1=queued, 2=sent, 3=failed) |
| error | NVARCHAR(500) | NULL |  |  |
| attempts | INT | NOT NULL | 0 |  |
| sent_at | DATETIME2(3) | NULL |  |  |
| created_at | DATETIME2(3) | NOT NULL | SYSUTCDATETIME() |  |

**Indexes:** `IX_email_log_status` on (`status`, `created_at`); `IX_email_log_order` on (`order_id`).

---

## 4.14 Hangfire tables

Hangfire creates and manages its own tables (`AggregatedCounter`, `Job`, `JobQueue`, `Hash`, `List`, `Set`, `Counter`, `Server`). They live in the same database. Do not modify them directly.

---

# 5. Indexes (Summary)

Hot-path indexes (beyond PKs and the per-table indexes above):

| Table | Index | Drives |
|-------|-------|--------|
| `variant` | `IX_variant_low_stock` (filtered) | Admin dashboard low-stock widget |
| `order` | `IX_order_state_changed` (filtered, pre-handoff states) | Hangfire 5-day auto-cancel scan |
| `order` | `IX_order_token` | Parent order-status page lookup |
| `order` | `IX_order_phone` | Counter pickup lookup by parent phone |
| `notify_request` | `IX_notify_pending_variant` (filtered, unfulfilled) | Restock email fan-out |
| `audit_log` | `IX_audit_created` (DESC) | Admin audit log browser |
| `order_item` | `IX_order_item_variant` | Exchange lookup, reporting |
| `product` | `IX_product_set` (filtered, in-set) | "Add full set" expansion |

---

# 6. Constraints (Summary)

**Referential integrity:** every FK is `ON DELETE NO ACTION` (soft delete via `is_archived` is the only delete path for catalog entities). `ON UPDATE NO ACTION` (PKs never mutate).

**Check constraints:**
- `variant.stock >= 0` — hard floor; the place-order transaction aborts on this.
- `variant.low_stock_threshold >= 0`
- `order_item.qty > 0`
- `exchange.qty > 0`
- `order.state IN (1..12)`, `order.channel IN (1, 2)`
- `school.type IN (1..6)`, `product.gender IN (1, 2, 3)`

**Unique constraints:**
- Active school name (filtered)
- (school_id, grade_stage name)
- item_type name
- Active product spec (school_id, grade_stage_id, item_type_id, gender)
- Active variant (product_id, size_label)
- Order tracking_token_hash
- Pending notify_request (variant_id, email)
- Admin email

---

# 7. Triggers vs Application Logic

**No triggers.** All state machine guards, stock arithmetic, audit logging, and notify-me fan-out live in the .NET application layer. Reasons:
- Triggers are invisible to the application and hard to unit-test.
- Audit log entries need the actor id (the admin or `system`), which the DB does not know.
- Hangfire enqueueing must happen outside the transaction (post-commit) for retries to work cleanly.
- Triggers would duplicate business rules across two layers.

The only DB-enforced logic is referential integrity, check constraints, and unique constraints.

---

# 8. Data Migration & Seeding

- **EF Core migrations**, forward-only, applied by CI in the deploy step.
- **Initial seed:** one `admin` row (owner's email + bcrypt-hashed password set out-of-band on first deploy).
- **Catalog seed:** none. The owner starts from zero (Q9) and keys everything in via the admin CRUD UI.
- **No CSV import in v1.** (Out of scope.)

---

# 9. Backup & Recovery

| Asset | Frequency | Retention | Mechanism |
|-------|-----------|-----------|-----------|
| MSSQL full backup | Nightly 02:00 | 14 days | MonsterASP scheduled backup to a second volume |
| MSSQL transaction log | Every 15 min | 48 h | MonsterASP native |
| `/uploads` (images) | Nightly 02:30 (rsync) | 14 days | MonsterASP scheduled |
| Restore test | Quarterly | n/a | Restore to staging slot, run smoke tests |

RPO: 15 minutes (transaction log). RTO: 1 hour (DB) / 2 hours (images). See SDD §6.4.

---

# 10. Scaling Strategy

| Trigger | Action |
|---------|--------|
| Admin dashboard p95 > 1 s | Add 1 read replica; route reports + dashboard reads to it |
| Peak write QPS > 50 | Audit connection pool + isolation; consider delayed-duration transactions |
| > 1 M variants | Shard by `school_id` (each school is naturally independent); cross-shard reporting via nightly export |
| Audit log > 10 M rows | Partition by `created_at` monthly; archive partitions older than 2 years to cold storage |
| `/uploads` > 50 GB | Move to Azure Blob Storage + Azure CDN; `product_image.url` becomes a CDN URL (no schema change) |

No premature sharding. No premature replication. Vertical first, then read replicas, then shard — explicit triggers, not vibes.
