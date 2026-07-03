# Admin — Orders

All require auth. Route prefix: `/api/v1/admin/orders`

## GET /api/v1/admin/orders
Query: `?state=<byte>&school=<long>&from=<datetime>&to=<datetime>&search=<string>&page=1&page_size=20`
- `search`: matches customer_phone (LIKE) or exact order id
Response: `{ items: [{ id, state, channel, customerName, customerPhone, total, createdAt, stateChangedAt }], total, page, page_size }`
- `state`: lowercase string (placed, in_transit, etc)
- `channel`: "delivery" | "pickup"
- Sorted by createdAt DESC

## GET /api/v1/admin/orders/{id}
Response 200:
```json
{
  "id": 1, "state": "placed", "channel": "delivery",
  "customerName": "...", "customerPhone": "...", "customerEmail": "...",
  "addressCity": "...", "addressLine": "...",
  "total": 120.00, "deliveryFee": 0, "bostaTrackingId": null,
  "createdAt": "...", "stateChangedAt": "...",
  "cancelledAt": null, "deliveredAt": null, "pickedUpAt": null,
  "handedToCourierAt": null, "inTransitAt": null, "returnedAt": null, "codFailedAt": null,
  "items": [{ "variantId": 1, "qty": 1, "unitPriceSnapshot": 120.00, "lineTotalSnapshot": 120.00, "sizeLabel": "M", "itemType": "T-Shirt", "color": null }],
  "timeline": [{ "action": "order.place", "createdAt": "...", "reason": null }]
}
```
404 if not found.

## POST /api/v1/admin/orders/{id}/transition
Body: `{ "toState": "ready_to_ship" }` (lowercase string)

Valid transitions:
```
placed → ready_to_ship, ready_for_pickup, cancelled
ready_to_ship → handed_to_courier, cancelled
ready_for_pickup → picked_up, cancelled
handed_to_courier → in_transit
in_transit → delivered, cod_failed
delivered → closed_success
cod_failed → returned_to_store
returned_to_store → closed_failed
picked_up → closed_success
```
Response 200: order object (same shape as detail, no timeline).
409: `{ error: "Invalid transition", from, to }` | 422: invalid state name

## POST /api/v1/admin/orders/{id}/bosta-pickup
Guard: state = ready_to_ship AND channel = delivery. Else 409.
Calls Bosta API, stores tracking ID, transitions to handed_to_courier, enqueues shipped email.
Response 200: order object | 502: `{ error: "bosta_error", detail }` on API failure

## POST /api/v1/admin/orders/{id}/mark-picked-up
Guard: state = ready_for_pickup AND channel = pickup. Else 409.
Transitions: ready_for_pickup → picked_up → closed_success. Sets pickedUpAt.
Response 200: order object

## POST /api/v1/admin/orders/{id}/exchanges
Body:
```json
{ "orderItemId": 1, "newVariantId": 2, "qty": 1, "reason": "size swap" }
```
Admin-only exchange: refunds old variant stock, deducts new variant stock, recalculates order total, inserts `exchange` row, audit log.
- 200: `{ "exchangeId": 1, "priceDelta": 10.00, "newTotal": 130.00, "cashSettlement": "parent_pays_10.00" }`
- 404: order not found
- 409: new variant out of stock
- 422: invalid orderItemId / newVariantId / qty

`cashSettlement`:
- `parent_pays_<abs>` — parent owes money
- `refund_parent_<abs>` — refund to parent
- `even` — no money changes hands

## POST /api/v1/admin/jobs/run-auto-cancel
Triggers auto-cancel job immediately (admin-only, for testing).
Response 200: `{ message: "Auto-cancel job completed" }`
Job cancels orders stale >5 days, refunds stock, enqueues cancel emails.
