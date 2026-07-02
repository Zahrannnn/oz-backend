# API Design Document

# School Uniform Store Platform

**Version:** 1.0
**Author:** Mohamed Zahran
**Status:** Draft (aligned with BRD v1.2 + PRD v1.2 + SDD v1.0 + DATABASE_DESIGN v1.0)
**Date:** July 2026
**Backend:** .NET 10 Web API (MonsterASP) — REST over HTTPS
**Frontend:** Next.js 16 on Vercel

---

# Table of Contents

1. Design Principles
2. Conventions
3. Authentication & Authorization
4. Error Format
5. Pagination, Filtering, Sorting
6. Idempotency
7. Rate Limiting
8. CORS & Security Headers
9. Public Storefront Endpoints
10. Public Order Endpoints
11. Webhook Endpoints
12. Admin — Authentication
13. Admin — Catalog
14. Admin — Inventory
15. Admin — Orders & State Machine
16. Admin — Exchanges
17. Admin — Reports
18. Admin — Audit Log
19. Status Code Reference
20. Open Questions

---

# 1. Design Principles

- **REST resource-oriented.** URLs name nouns; HTTP verbs name actions. State changes use `POST /resource/{id}/sub-action` for non-CRUD transitions.
- **JSON only.** `Content-Type: application/json; charset=utf-8`. UTF-8 throughout (Arabic-safe).
- **Versioning.** All paths prefixed with `/api/v1/`. Breaking changes bump to `/v2/`.
- **Stateless backend.** Every request carries its own auth (JWT cookie for admin; none for public). No server session.
- **Idempotent writes.** Order placement and stock edits accept an `Idempotency-Key` header.
- **Consistent error shape.** RFC 7807 ProblemDetails (`application/problem+json`).
- **No HATEOAS.** The Next.js frontend owns navigation; the API returns plain JSON.
- **No GraphQL.** REST is enough for v1; the surface is small.
- **Soft delete only.** `DELETE` verbs return `405`; archiving uses `POST /resource/{id}/archive`.
- **UTC timestamps.** ISO 8601 with `Z` suffix; the frontend converts to Egypt local time for display.

---

# 2. Conventions

| Convention | Choice |
|------------|--------|
| Base URL | `https://api.<domain>/api/v1` |
| Auth (admin) | JWT in `httpOnly; Secure; SameSite=Lax` cookie named `admin_session` |
| Auth (public) | None |
| Pagination | `?page=1&page_size=20` (max 100); response carries `total`, `page`, `page_size` |
| Filtering | `?field=value`; comma-separated for OR (`?state=placed,ready_to_ship`) |
| Sorting | `?sort=created_at` (asc) or `?sort=-created_at` (desc) |
| Idempotency | `Idempotency-Key: <uuid4>` header on `POST /orders` and stock edits |
| Date params | `?from=2026-07-01T00:00:00Z&to=2026-07-31T23:59:59Z` |
| Locale | Responses are locale-agnostic; UI translation is frontend-only |
| Field casing | `camelCase` in JSON |

---

# 3. Authentication & Authorization

## 3.1 Public endpoints

No auth header. Examples: catalog reads, order placement, order-status lookup by token, notify-me capture, Bosta webhook.

## 3.2 Admin endpoints

Require a valid JWT in the `admin_session` cookie. The cookie is set by `POST /api/v1/admin/auth/login` and renewed on every admin request (8-hour sliding expiry).

- **Unauthorized:** any admin endpoint without a valid cookie → `401`.
- **Forbidden:** not used in v1 (single admin, no roles).
- **Login failure rate limit:** 5 failed attempts per email per 15 min → `429`.

---

# 4. Error Format

All non-2xx responses use RFC 7807 ProblemDetails:

```json
{
  "type": "https://api.<domain>/errors/out-of-stock",
  "title": "Out of stock",
  "status": 409,
  "detail": "Variant 1234 has 0 units available; requested 1.",
  "instance": "/api/v1/orders",
  "traceId": "00-abc123def456-1",
  "errors": {
    "items[0].variantId": "out_of_stock"
  }
}
```

Common error types:

| `type` suffix | Status | Meaning |
|---------------|--------|---------|
| `validation-error` | 422 | Request body failed validation |
| `not-found` | 404 | Resource does not exist or is archived |
| `conflict` | 409 | State machine or uniqueness violation |
| `out-of-stock` | 409 | Place-order variant check failed |
| `unauthorized` | 401 | Missing or invalid admin cookie |
| `rate-limited` | 429 | Rate limit exceeded |
| `bosta-error` | 502 | Upstream Bosta call failed |
| `internal-error` | 500 | Unexpected; `traceId` for log lookup |

---

# 5. Pagination, Filtering, Sorting

Paginated list responses share one shape:

```json
{
  "data": [ ],
  "total": 134,
  "page": 1,
  "page_size": 20,
  "has_next": true
}
```

- `page` is 1-based.
- `page_size` defaults to 20, capped at 100.
- `total` is the unfiltered count for the current filter set.
- Sorting uses `?sort=-created_at,name` (descending created_at, ascending name).

---

# 6. Idempotency

`POST /api/v1/orders` and `POST /api/v1/admin/variants/{id}/stock` accept an `Idempotency-Key` header (UUID v4).

- Same key within 24 h → server returns the cached original response without re-executing.
- Without the header → request runs normally (no double-submit protection).
- Frontend generates a fresh UUID per user action, not per HTTP retry.

---

# 7. Rate Limiting

| Bucket | Limit | Window | Key |
|--------|-------|--------|-----|
| Public reads | 100 req | 10 s | IP |
| Order placement | 5 req | 10 s | IP |
| Notify-me capture | 10 req | 10 s | IP |
| Admin login | 5 attempts | 15 min | email |
| Admin API | 60 req | 10 s | admin id |

Exceeded limits → `429 Too Many Requests` with `Retry-After: <seconds>`.

Implemented via ASP.NET Core `RateLimiter` middleware.

---

# 8. CORS & Security Headers

**CORS:** `Access-Control-Allow-Origin: https://<vercel-origin>` only. No credentials on public endpoints; `Allow-Credentials: true` for admin cookie.

**Security headers** (added by middleware):
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Content-Security-Policy: default-src 'self'` (tuned per route)
- `Permissions-Policy: geolocation=(), microphone=(), camera=()`

---

# 9. Public Storefront Endpoints

## 9.1 List schools

`GET /api/v1/schools`

Query: `?q=<name>&type=<1..6>&page=1&page_size=20`

Response 200:
```json
{
  "data": [
    { "id": 1, "name": "Azhar Eldelta", "type": 3, "typeLabel": "Azhar_Eldelta" }
  ],
  "total": 30, "page": 1, "page_size": 20, "has_next": false
}
```

Notes: Full list (~30 rows) is cached client-side by MiniSearch; `q` runs the MSSQL `LIKE` fallback for server-side filtering.

---

## 9.2 List grade-stages for a school

`GET /api/v1/schools/{schoolId}/grade-stages`

Response 200:
```json
{
  "data": [
    { "id": 11, "schoolId": 1, "name": "Ebtda2y", "displayOrder": 1 }
  ]
}
```

---

## 9.3 List products for a school + grade-stage

`GET /api/v1/schools/{schoolId}/products?gradeStageId={id}&gender={1|2|3}`

Query: `?itemTypeId=<id>&page=1&page_size=20`

Response 200 (product card):
```json
{
  "data": [
    {
      "id": 101,
      "itemType": "Trousers",
      "gender": 1,
      "color": "Grey",
      "isInSet": true,
      "priceFrom": 120.00,
      "thumbnailUrl": "/uploads/2026/07/abc.jpg",
      "stockStatus": "in_stock",
      "variants": [
        { "id": 1001, "sizeLabel": "8", "priceInclVat": 120.00, "stock": 12 }
      ]
    }
  ],
  "total": 10, "page": 1, "page_size": 20, "has_next": false
}
```

`stockStatus`: `in_stock` | `low_stock` | `out_of_stock`. `priceFrom` is the minimum variant price.

---

## 9.4 Get product detail

`GET /api/v1/products/{id}`

Response 200:
```json
{
  "id": 101,
  "schoolId": 1,
  "schoolName": "Azhar Eldelta",
  "gradeStageId": 11,
  "gradeStageName": "Ebtda2y",
  "itemType": "Trousers",
  "gender": 1,
  "color": "Grey",
  "isInSet": true,
  "images": [
    { "id": 5, "url": "/uploads/2026/07/abc.jpg", "sortOrder": 0 }
  ],
  "variants": [
    { "id": 1001, "sizeLabel": "8", "priceInclVat": 120.00, "stock": 12, "lowStock": false }
  ]
}
```

---

## 9.5 Get full-set members (for "Add full set" UX)

`GET /api/v1/schools/{schoolId}/set?gradeStageId={id}&gender={1|2}`

Response 200:
```json
{
  "data": [
    { "productId": 101, "variantId": 1001, "sizeLabel": "8", "priceInclVat": 120.00 }
  ]
}
```

Notes: Returns only `is_in_set = 1` products for the (school, grade, gender). Frontend expands to cart line items at individual prices; no bundle SKU.

---

## 9.6 Capture notify-me request

`POST /api/v1/products/{id}/notify-me`

Body:
```json
{ "variantId": 1001, "email": "parent@example.com" }
```

Responses:
- `201 Created` — new request stored.
- `409 conflict` — duplicate pending request for the same `(variantId, email)`; treated as success by the frontend (idempotent on the parent side).
- `422 validation-error` — invalid email or missing variant.

---

# 10. Public Order Endpoints

## 10.1 Place order

`POST /api/v1/orders`

Header: `Idempotency-Key: <uuid4>` (recommended)

Body:
```json
{
  "channel": "delivery",
  "customer": {
    "name": "Ahmed Ali",
    "phone": "+201234567890",
    "email": "parent@example.com",
    "addressCity": "Cairo",
    "addressLine": "12 Tahrir St, Apt 3"
  },
  "items": [
    { "variantId": 1001, "qty": 1 }
  ]
}
```

For pickup orders: `channel = "pickup"`, `addressLine` omitted.

Responses:
- `201 Created`:
```json
{
  "orderId": 5001,
  "token": "k3N...random",
  "trackingUrl": "https://<domain>/orders/k3N...random",
  "total": 145.00,
  "state": "placed"
}
```
- `409 out-of-stock`:
```json
{
  "type": ".../errors/out-of-stock",
  "status": 409,
  "detail": "Variant 1001 has 0 units available; requested 1.",
  "errors": { "items[0].variantId": "out_of_stock" }
}
```
- `422 validation-error` — invalid phone, empty cart, etc.

Server-side transaction: lock variant rows, check stock, insert order + items, decrement stock, enqueue confirmation email, commit.

---

## 10.2 Get order status (by token)

`GET /api/v1/orders/by-token/{token}`

Response 200:
```json
{
  "orderId": 5001,
  "state": "in_transit",
  "stateLabel": "In transit",
  "channel": "delivery",
  "total": 145.00,
  "createdAt": "2026-07-02T10:00:00Z",
  "bostaTrackingId": "BST-12345",
  "timeline": [
    { "state": "placed", "at": "2026-07-02T10:00:00Z" },
    { "state": "ready_to_ship", "at": "2026-07-02T11:00:00Z" },
    { "state": "handed_to_courier", "at": "2026-07-02T12:00:00Z" },
    { "state": "in_transit", "at": "2026-07-02T13:00:00Z" }
  ],
  "items": [
    { "variantId": 1001, "qty": 1, "unitPriceSnapshot": 120.00, "sizeLabel": "8", "itemType": "Trousers", "color": "Grey" }
  ]
}
```

Response 404 — token not found or expired. Rate-limited per IP to prevent token enumeration.

---

## 10.3 Cancel order (parent-initiated, pre-handoff)

`POST /api/v1/orders/by-token/{token}/cancel`

Body:
```json
{ "reason": "Changed mind" }
```

Responses:
- `200 OK` — order transitioned to `cancelled`, stock refunded.
- `409 conflict` — order is past the cancellable state (`handed_to_courier` / `picked_up` / terminal).

---

# 11. Webhook Endpoints

## 11.1 Bosta tracking webhook

`POST /api/v1/webhooks/bosta`

Header: `X-Bosta-Signature: <hex HMAC-SHA256 of body>`

Body (Bosta-defined shape):
```json
{
  "trackingId": "BST-12345",
  "status": "delivered",
  "timestamp": "2026-07-03T09:00:00Z"
}
```

Server-side:
1. Verify HMAC against shared secret. `401` on mismatch.
2. Map Bosta `status` to order state transition:
   - `in_transit` → no-op if already `in_transit`; advance from `handed_to_courier` if needed.
   - `delivered` → `in_transit → delivered → closed_success`; set `delivered_at`; enqueue `OrderDeliveredEmail`.
   - `cod_failed` → `in_transit → cod_failed`; enqueue `CodFailedEmail`.
   - `returned_to_store` → `cod_failed → returned_to_store → closed_failed`; refund stock.
3. Idempotent: re-delivery of the same status is a no-op.

Responses: `200 OK` on success; `401` on bad signature; `404` if tracking id unknown; `422` if status not mappable.

---

# 12. Admin — Authentication

## 12.1 Login

`POST /api/v1/admin/auth/login`

Body:
```json
{ "email": "owner@store.com", "password": "..." }
```

Response 200: sets `admin_session` cookie (httpOnly, Secure, SameSite=Lax, 8h sliding expiry). Body:
```json
{ "adminId": "uuid", "email": "owner@store.com", "expiresAt": "2026-07-02T18:00:00Z" }
```

Response 401 on bad credentials. Rate-limited (5 / 15 min / email). Audit log entry on both success and failure.

---

## 12.2 Logout

`POST /api/v1/admin/auth/logout`

Response 200: clears the `admin_session` cookie.

---

## 12.3 Get current session

`GET /api/v1/admin/auth/me`

Response 200:
```json
{ "adminId": "uuid", "email": "owner@store.com", "expiresAt": "2026-07-02T18:00:00Z" }
```

Response 401 if no valid cookie.

---

## 12.4 Reset password with recovery code

`POST /api/v1/admin/auth/reset-password`

Body:
```json
{ "email": "owner@store.com", "recoveryCode": "...", "newPassword": "..." }
```

Response 200 — password updated, recovery code consumed (one-time), audit log entry. Response 409 if recovery code already used or invalid.

---

## 12.5 Display recovery code (first-login flow)

`POST /api/v1/admin/auth/recovery-code`

Called once after first login. Response 200:
```json
{ "recoveryCode": "XXXX-XXXX-XXXX-XXXX" }
```

The code is hashed at rest (`recovery_code_hash`); the plaintext is shown only this once. Subsequent calls return `409 conflict`.

---

# 13. Admin — Catalog

All endpoints require the admin cookie. Standard CRUD shape per resource.

## 13.1 Schools

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/admin/schools` | List (paginated, filterable) |
| POST | `/admin/schools` | Create |
| GET | `/admin/schools/{id}` | Get one |
| PUT | `/admin/schools/{id}` | Update name / type |
| POST | `/admin/schools/{id}/archive` | Soft-delete (`is_archived = 1`) |

School body:
```json
{ "name": "Azhar Eldelta", "type": 3 }
```

## 13.2 Grade-stages

| Method | Path |
|--------|------|
| GET | `/admin/schools/{schoolId}/grade-stages` |
| POST | `/admin/schools/{schoolId}/grade-stages` |
| PUT | `/admin/grade-stages/{id}` |
| POST | `/admin/grade-stages/{id}/archive` |

Body: `{ "name": "Ebtda2y", "displayOrder": 1 }`.

## 13.3 Item-types

| Method | Path |
|--------|------|
| GET | `/admin/item-types` |
| POST | `/admin/item-types` |
| PUT | `/admin/item-types/{id}` |

Body: `{ "name": "Trousers" }`. No archive in v1 (shared reference; renaming is the only mutation).

## 13.4 Products

| Method | Path |
|--------|------|
| GET | `/admin/products` |
| POST | `/admin/products` |
| GET | `/admin/products/{id}` |
| PUT | `/admin/products/{id}` |
| POST | `/admin/products/{id}/archive` |
| PUT | `/admin/products/{id}/set-flag` |

Body (create / update):
```json
{
  "schoolId": 1,
  "gradeStageId": 11,
  "itemTypeId": 5,
  "gender": 1,
  "color": "Grey",
  "isInSet": true
}
```

`PUT /admin/products/{id}/set-flag` body: `{ "isInSet": true }` — toggles the full-set membership flag only.

## 13.5 Variants

| Method | Path |
|--------|------|
| POST | `/admin/products/{productId}/variants` |
| PUT | `/admin/variants/{id}` |
| POST | `/admin/variants/{id}/archive` |

Body: `{ "sizeLabel": "8", "priceInclVat": 120.00, "lowStockThreshold": 5 }`. Stock is set via the inventory endpoint (§14), not here.

## 13.6 Product images

| Method | Path |
|--------|------|
| POST | `/admin/products/{productId}/images` |
| PUT | `/admin/product-images/{id}` |
| DELETE | `/admin/product-images/{id}` (real delete — image rows are cheap) |

Upload via `multipart/form-data`:
```
POST /admin/products/{productId}/images
Content-Type: multipart/form-data

file: <binary>
sortOrder: 0
```

Server stores the file under `/uploads/{yyyy}/{mm}/{guid}.jpg` on the backend host and inserts a `product_image` row with the relative URL. Response:
```json
{ "id": 9, "productId": 101, "url": "/uploads/2026/07/abc.jpg", "sortOrder": 0 }
```

---

# 14. Admin — Inventory

## 14.1 Manual stock edit

`POST /api/v1/admin/variants/{id}/stock`

Header: `Idempotency-Key: <uuid4>`

Body:
```json
{ "newStock": 50, "reason": "Received from supplier" }
```

Response 200:
```json
{
  "variantId": 1001,
  "oldStock": 0,
  "newStock": 50,
  "notifyMeTriggered": true,
  "notifyMeCount": 7,
  "lowStock": false
}
```

Server-side:
1. Open transaction, `SELECT … WITH (UPDLOCK, ROWLOCK)`, capture `oldStock`.
2. `UPDATE variant SET stock = @newStock`.
3. Audit log row.
4. Commit.
5. Outside the transaction: if `oldStock == 0 AND newStock > 0`, enqueue `NotifyRestockEmailJob`. If `newStock < low_stock_threshold`, flag for the dashboard widget.

Response `422 validation-error` if `newStock < 0`.

## 14.2 Update low-stock threshold

`PUT /api/v1/admin/variants/{id}/threshold`

Body: `{ "lowStockThreshold": 3 }`.

## 14.3 Variant stock history (audit log view)

`GET /api/v1/admin/variants/{id}/stock-history`

Returns audit log rows filtered to `action = 'stock.edit'` and `entity_id = <variantId>`, newest first.

---

# 15. Admin — Orders & State Machine

## 15.1 List orders

`GET /api/v1/admin/orders`

Query: `?state=placed,ready_to_ship&schoolId=1&from=...&to=...&q=<phone-or-id>&page=1&page_size=20`

Response: paginated order summary rows (id, state, channel, customer name + phone, total, createdAt, stateChangedAt).

## 15.2 Get order detail

`GET /api/v1/admin/orders/{id}`

Full order with items, exchanges, email log, audit entries.

## 15.3 State transition

`POST /api/v1/admin/orders/{id}/transition`

Body:
```json
{ "toState": "ready_to_ship", "reason": "Packed" }
```

Response 200 — order in the new state. Response `409 conflict` if `{fromState, toState}` is not in the whitelist.

Whitelist:
- `placed → ready_to_ship`
- `ready_to_ship → handed_to_courier` (only via Bosta booking, see §15.4)
- `ready_to_ship → ready_for_pickup`
- `ready_for_pickup → picked_up`
- `picked_up → closed_success`
- `delivered → closed_success`
- Any pre-handoff state → `cancelled` (parent or admin)

The `handed_to_courier`, `in_transit`, `delivered`, `cod_failed`, `returned_to_store`, `closed_failed`, `closed_success` transitions come from the Bosta webhook (§11.1) or auto-cancel job, not from this endpoint.

## 15.4 Book Bosta pickup

`POST /api/v1/admin/orders/{id}/bosta-pickup`

Body: `{ }` (no params; server uses the order's address + total COD amount)

Response 200:
```json
{ "bostaTrackingId": "BST-12345", "newState": "handed_to_courier" }
```

Side effects: calls Bosta `CreateShipment`, stores `bosta_tracking_id`, transitions `ready_to_ship → handed_to_courier`, enqueues `OrderShippedEmail` with the token URL.

Response `502 bosta-error` on upstream failure; order state unchanged; admin retries.

## 15.5 Mark picked up (counter flow)

`POST /api/v1/admin/orders/{id}/mark-picked-up`

Body: `{ }`

Response 200: `picked_up → closed_success`. Audit log entry. Stock is NOT decremented again (already done at `placed`).

Lookup helper:
- `GET /api/v1/admin/orders/by-phone?phone=<...>` — list today's `ready_for_pickup` orders matching the phone.
- `GET /api/v1/admin/orders/{id}` by order number (admin already has this).

## 15.6 Cancel order (admin)

`POST /api/v1/admin/orders/{id}/cancel`

Body: `{ "reason": "Customer request" }`

Response 200: order → `cancelled`, stock refunded. Response `409` if past cancellable state.

## 15.7 Print invoice

`GET /api/v1/admin/orders/{id}/invoice`

Response 200 `Content-Type: application/pdf` — server-rendered invoice (QuestPDF or similar). Used at the counter for pickup orders.

---

# 16. Admin — Exchanges

## 16.1 Log an exchange

`POST /api/v1/admin/orders/{id}/exchanges`

Body:
```json
{
  "orderItemId": 7001,
  "newVariantId": 1002,
  "qty": 1,
  "reason": "Size too small"
}
```

Server-side transaction:
1. Lock old + new variant rows.
2. `UPDATE variant SET stock = stock + @qty WHERE id = @old_variant_id`.
3. `UPDATE variant SET stock = stock - @qty WHERE id = @new_variant_id`. CHECK `stock >= 0`; abort → `409 out-of-stock`.
4. Compute `price_delta = (new_variant.price_incl_vat - order_item.unit_price_snapshot) * qty`.
5. Insert `exchange` row referencing `order_item_id`.
6. `UPDATE order SET total = total + @price_delta WHERE id = @order_id`.
7. Audit log row.
8. Commit.

Response 200:
```json
{
  "exchangeId": 9001,
  "orderId": 5001,
  "newTotal": 165.00,
  "priceDelta": 20.00,
  "cashSettlement": "parent_pays_20"
}
```

`cashSettlement`: `parent_pays_<amount>` | `parent_refunded_<amount>` | `even` — drives the in-store cash flow.

Response `422` if `orderItemId` does not belong to the order. Response `409` if new variant is OOS.

## 16.2 List exchanges for an order

`GET /api/v1/admin/orders/{id}/exchanges`

Response: array of exchange rows with old/new variant details and deltas.

---

# 17. Admin — Reports

All reports return JSON (no PDF/CSV in v1; CSV export only for the audit log).

## 17.1 Sales summary

`GET /api/v1/admin/reports/sales?from=...&to=...&groupBy=day|week|month&schoolId=<optional>`

Response:
```json
{
  "from": "2026-07-01T00:00:00Z",
  "to": "2026-07-31T23:59:59Z",
  "groupBy": "day",
  "rows": [
    { "bucket": "2026-07-01", "ordersCount": 12, "revenue": 1740.00, "avgOrderValue": 145.00, "byChannel": { "delivery": 10, "pickup": 2 } }
  ],
  "totals": { "ordersCount": 360, "revenue": 52400.00, "avgOrderValue": 145.55 }
}
```

## 17.2 Inventory status

`GET /api/v1/admin/reports/inventory?schoolId=<optional>&onlyLowStock=true`

Response:
```json
{
  "totalVariants": 10800,
  "lowStockCount": 42,
  "outOfStockCount": 7,
  "rows": [
    { "variantId": 1001, "productLabel": "Azhar Eldelta / Ebtda2y / Trousers / Boys / 8", "stock": 2, "lowStockThreshold": 5, "status": "low_stock" }
  ]
}
```

`onlyLowStock=true` filters to `stock < low_stock_threshold`.

## 17.3 Order list

`GET /api/v1/admin/reports/orders?state=...&schoolId=...&from=...&to=...`

Same shape as `/admin/orders` but with heavier aggregates (revenue per state, COD-failed rate).

## 17.4 Notify-me demand

`GET /api/v1/admin/reports/notify-me?from=...&to=...`

Response:
```json
{
  "totalPending": 240,
  "rows": [
    { "variantId": 1001, "productLabel": "...", "pendingCount": 18 }
  ]
}
```

Sorted by `pendingCount DESC` — tells the owner which OOS variants to restock first.

---

# 18. Admin — Audit Log

## 18.1 Browse audit log

`GET /api/v1/admin/audit-log?actorId=<optional>&action=<optional>&entityType=<optional>&from=...&to=...&page=1&page_size=50`

Response: paginated rows:
```json
{
  "data": [
    {
      "id": 12345,
      "actorId": "uuid",
      "actorEmail": "owner@store.com",
      "action": "order.transition",
      "entityType": "order",
      "entityId": "5001",
      "beforeJson": "{ \"state\": \"placed\" }",
      "afterJson": "{ \"state\": \"ready_to_ship\" }",
      "reason": "Packed",
      "createdAt": "2026-07-02T11:00:00Z"
    }
  ],
  "total": 5000, "page": 1, "page_size": 50, "has_next": true
}
```

## 18.2 CSV export

`GET /api/v1/admin/audit-log/export?...same filters...`

Response 200 `Content-Type: text/csv` with `Content-Disposition: attachment; filename="audit-log-<date>.csv"`.

---

# 19. Status Code Reference

| Code | Meaning | Used by |
|------|---------|---------|
| 200 | OK | All successful reads and most successful writes |
| 201 | Created | `POST /orders`, `POST /notify-me`, `POST /admin/...` creates |
| 204 | No Content | Logout, archive actions |
| 400 | Bad Request | Malformed JSON (rare; validation usually 422) |
| 401 | Unauthorized | Admin endpoint without valid cookie; Bosta webhook bad signature |
| 404 | Not Found | Resource does not exist or is archived; order token invalid |
| 405 | Method Not Allowed | `DELETE` on any resource (use archive) |
| 409 | Conflict | State machine violation, uniqueness violation, OOS at place-time, duplicate notify-me |
| 422 | Validation Error | Body failed DTO validation |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Error | Unexpected; `traceId` in body |
| 502 | Bad Gateway | Upstream Bosta call failed |
| 503 | Service Unavailable | Down for maintenance |

---

# 20. Open Questions

- Exact Bosta webhook payload shape (Bosta account still pending; ops question).
- Bosta pickup slot window (fixed slot vs flexible N hours) — affects admin booking UX.
- Invoice PDF library choice (QuestPDF vs iText vs Puppeteer-on-Vercel). Leaning QuestPDF for .NET-native, MIT-licensed.
- Recovery code format and length (4×4 alphanumeric suggested; confirm during impl).
- Whether `/admin/orders/by-phone` should also match archived / historical orders or only today's `ready_for_pickup` (v1: today's only).
- Exact CSP for the admin SPA once the frontend is built (default-src 'self' as starting point).

