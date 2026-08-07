# Storefront — Orders

## POST /api/v1/orders
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

Side effects: enqueues customer confirmation email and, if `ADMIN_NOTIFY_EMAIL` (env) / `Admin:NotifyEmail` (config) is set, enqueues admin new-order notification email with order summary and dashboard link.

## GET /api/v1/orders/by-token/{token}
Response 200:
```json
{
  "orderId": 1, "state": "placed", "stateLabel": "Placed", "channel": "delivery",
  "total": 120.00, "createdAt": "2026-07-02T15:25:35.732", "bostaTrackingId": null,
  "timeline": [{ "state": "place", "at": "2026-07-02T15:25:35.891" }],
  "items": [{ "variantId": 1, "qty": 1, "unitPriceSnapshot": 120.00, "sizeLabel": "M", "itemType": "T-Shirt", "color": null }]
}
```
404 if token doesn't match.

## POST /api/v1/orders/by-token/{token}/cancel
Body: `{ "reason": "string (optional)" }`

Parent self-service cancel. Refunds stock, sends cancel email, writes audit log.
- 200: `{ "state": "cancelled" }`
- 404: token doesn't match
- 409: order state not in {placed, ready_to_ship, ready_for_pickup}

Order states: `placed` | `ready_to_ship` | `handed_to_courier` | `in_transit` | `delivered` | `cod_failed` | `returned_to_store` | `ready_for_pickup` | `picked_up` | `closed_success` | `closed_failed` | `cancelled`
