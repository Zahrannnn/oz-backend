# API Reference — Oz Backend

Base URL: `http://localhost:5000` (dev)

Auth:
- Public endpoints: no auth
- Admin endpoints: `Authorization: Bearer <token>` header OR `admin_session` cookie (HttpOnly)
- Login at `POST /api/v1/admin/auth/login` to get token

Pagination: `{ items: [], total: int, page: int, page_size: int }`
Query params: `?page=1&page_size=20` (max: 100)

Errors: RFC 7807 — `{ type, title, status, detail, errors? }`

---

## Health

### GET /api/v1/health
No auth. Response: `{ status: "ok" }`

### GET /api/v1/readyz
No auth. Responses: `{ status, timestamp }` (200) | `{ status: "unhealthy", failures: [] }` (503)

---

## Storefront — Schools

### GET /api/v1/schools
Query: `?q=search_text&page=1&page_size=20`
Response 200:
```json
{ "items": [{ "id": 1, "name": "Cairo Language School", "type": "Experimental", "typeLabel": "Experimental" }], "total": 3, "page": 1, "page_size": 20 }
```

### GET /api/v1/schools/{id}
Response 200: same item shape. 404 if missing.

---

## Storefront — Grade Stages

### GET /api/v1/schools/{schoolId}/grade-stages
Response 200:
```json
{ "items": [{ "id": 1, "schoolId": 1, "name": "KG1", "displayOrder": 1 }] }
```

---

## Storefront — Products

### GET /api/v1/schools/{schoolId}/grade-stages/{gradeId}/products
Query: `?item_type=1&gender=1&page=1&page_size=20`

Response 200:
```json
{
  "items": [{
    "id": 1, "itemType": "T-Shirt", "gender": 1, "color": "\u0627\u062d\u0645\u0631",
    "isInSet": false, "priceFrom": 120.00, "thumbnailUrl": null,
    "stockStatus": "in_stock",
    "variants": [{ "id": 1, "sizeLabel": "M", "priceInclVat": 120.00, "stock": 7 }]
  }], "total": 1, "page": 1, "page_size": 20, "has_next": false
}
```

Stock status values: `in_stock` | `low_stock` | `out_of_stock`

### GET /api/v1/products/{id}
Response 200:
```json
{
  "id": 1, "schoolName": "Cairo Language School", "gradeStageName": "KG1",
  "itemType": "T-Shirt", "gender": 1, "color": null, "isInSet": false,
  "variants": [{ "id": 1, "sizeLabel": "M", "priceInclVat": 120.00, "stock": 7 }],
  "images": [], "createdAt": "...", "updatedAt": "..."
}
```

---

## Storefront — Orders

### POST /api/v1/orders
Body:
```json
{
  "channel": "delivery",
  "customer": { "name": "Ahmed Ali", "phone": "01234567890", "email": "parent@example.com", "addressCity": "Cairo", "addressLine": "12 Tahrir St" },
  "items": [{ "variantId": 1, "qty": 1 }]
}
```

For pickup: `"channel": "pickup"`, omit `addressLine`.

Responses:
- 201: `{ "orderId": 1, "token": "base64url-token", "trackingUrl": "http://localhost:5000/orders/...", "total": 120.00, "state": "placed" }`
- 409 (out of stock): `{ "type": ".../errors/out-of-stock", "status": 409, "detail": "Variant 1 has 0 units available; requested 1.", "errors": { "items[.](variantId=1)": "out_of_stock" } }`
- 422: invalid channel, empty items, variantId <= 0, qty <= 0

### GET /api/v1/orders/by-token/{token}
Response 200:
```json
{
  "orderId": 1, "state": "placed", "stateLabel": "Placed", "channel": "delivery",
  "total": 120.00, "createdAt": "2026-07-02T15:25:35.732", "bostaTrackingId": null,
  "timeline": [{ "state": "place", "at": "2026-07-02T15:25:35.891" }],
  "items": [{ "variantId": 1, "qty": 1, "unitPriceSnapshot": 120.00, "sizeLabel": "M", "itemType": "T-Shirt", "color": null }]
}
```

Order states: `placed` | `ready_to_ship` | `handed_to_courier` | `in_transit` | `delivered` | `cod_failed` | `returned_to_store` | `ready_for_pickup` | `picked_up` | `closed_success` | `closed_failed` | `cancelled`

---

## Admin — Auth

### POST /api/v1/admin/auth/login
Body: `{ "email": "admin@oz.com", "password": "admin123" }`

Response 200:
```json
{ "adminId": "guid", "email": "admin@oz.com", "token": "jwt...", "expiresAt": "..." }
```
Sets `admin_session` cookie (HttpOnly, Secure, SameSite=Lax, 8h).

Errors: 401 `{ error: "invalid_credentials" }` | 423 `{ error: "account_locked", lockedUntil: "..." }`

### POST /api/v1/admin/auth/logout
Response 200: `{ message: "logged_out" }`

### GET /api/v1/admin/auth/me
Requires auth. Response 200: `{ adminId, email, expiresAt }` | 401

---

## Admin — Schools

All require auth (`[Authorize]`). Route prefix: `/api/v1/admin/schools`

### GET /api/v1/admin/schools
Query: `?page=1&page_size=20`
Response: `{ items: [SchoolDto], total, page, pageSize }`

### GET /api/v1/admin/schools/{id}
Response: SchoolDto

### POST /api/v1/admin/schools
Body: `{ "name": "New School", "type": 1 }` (type: 1=National, 2=Experimental, 3=Arabic, 4=Language, 5=International, 6=Private)

### PUT /api/v1/admin/schools/{id}
Body: same as POST

### POST /api/v1/admin/schools/{id}/archive
Soft delete. Response: `{ message: "School archived" }`

### DELETE /api/v1/admin/schools/{id}
Returns 405. Use archive instead.

SchoolDto shape: `{ id, name, type, isArchived, createdAt }`

---

## Admin — Products

All require auth. Route prefix: `/api/v1/admin/products`

### GET /api/v1/admin/products
Query: `?schoolId=1&gradeStageId=1&itemTypeId=1&gender=1&page=1&page_size=20`
Response:
```json
{ "items": [{ "id": 1, "schoolName": "Cairo Language School", "gradeStageName": "KG1", "itemType": "T-Shirt", "gender": 1, "color": "...", "isInSet": false, "isArchived": false, "createdAt": "...", "updatedAt": "..." }], "total": 1, "page": 1, "page_size": 20 }
```

### POST /api/v1/admin/products
Body: `{ schoolId, gradeStageId, itemTypeId, gender, color? }` (gender: 1=Boys, 2=Girls, 3=Unisex)
Response 201: AdminProductDto

### PUT /api/v1/admin/products/{id}
Body: same as POST

### POST /api/v1/admin/products/{id}/archive
Soft delete.

### PUT /api/v1/admin/products/{id}/set-flag
Toggles `isInSet`.

### POST /api/v1/admin/products/{productId}/variants
Body: `{ sizeLabel, priceInclVat, stock?, reserved?, lowStockThreshold? }`
Response 201: `{ id, productId, sizeLabel, priceInclVat, stock, reserved, lowStockThreshold, isArchived, createdAt, updatedAt }`

### PUT /api/v1/admin/variants/{id}
Body: `{ sizeLabel, priceInclVat, stock, reserved, lowStockThreshold }`

### POST /api/v1/admin/variants/{id}/archive
Soft delete.

---

## Admin — Product Images

All require auth. Route prefix: `/api/v1/admin/products/{productId}/images`

### POST /api/v1/admin/products/{productId}/images
Multipart form-data, field name `file`. Max 5 MB. Allowed: jpg, jpeg, png, webp.
Response 201: `{ id, productId, url, sortOrder }`

### DELETE /api/v1/admin/products/{productId}/images/{imageId}
Response 204: NoContent

---

## Entity Field Reference

| Field | Type | Notes |
|-------|------|-------|
| Gender | byte | 1=Boys, 2=Girls, 3=Unisex |
| SchoolType | byte | 1=National, 2=Experimental, 3=Arabic, 4=Language, 5=International, 6=Private |
| color | string | Arabic text (e.g. "احمر") |
| priceInclVat | decimal | Price including VAT |
| stock | int | Current inventory |
| deliveryFee | decimal | Currently 0 (free) |
| trackingUrl | string | `{base}/orders/{token}` |
