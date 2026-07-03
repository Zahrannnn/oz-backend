# Webhooks

## POST /api/v1/webhooks/bosta
No auth header — uses HMAC-SHA256 signature verification.

Headers:
- `X-Bosta-Signature`: hex HMAC-SHA256 of body, keyed by `BOSTA_WEBHOOK_SECRET` env var

Body: `{ "trackingId": "BST-12345", "status": "delivered" }`

Status mappings:
| Bosta status | Order transition | Side effects |
|-------------|-----------------|--------------|
| `in_transit` | handed_to_courier → in_transit | sets inTransitAt |
| `delivered` | in_transit → delivered → closed_success | sets deliveredAt, enqueues delivered email |
| `cod_failed` | in_transit → cod_failed | sets codFailedAt, enqueues cod_failed email |
| `returned_to_store` | cod_failed → returned_to_store → closed_failed | refunds stock, enqueues cancel email |

Idempotent: re-delivery of same status is no-op (state already advanced).

Response 200: `{ status: "ok" }` | 401: bad signature | 404: order not found
