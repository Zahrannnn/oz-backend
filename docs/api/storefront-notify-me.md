# Storefront — Notify Me

Public endpoints. No auth.

## POST /api/v1/variants/{id}/notify-me
Body: `{ "email": "parent@example.com" }`

Subscribe to a back-in-stock alert for an out-of-stock variant.
- Guard: variant must exist and `stock == 0`. Else 409 `{ "error": "Variant is in stock" }`
- Dedup: existing un-notified subscription for the same `(variantId, email_hash)` → 409 `{ "error": "Already subscribed" }`
- 201: subscription stored
- 422: invalid email format

Email hash: SHA-256 of normalized email, lowercase hex.

Notifications are sent automatically when an admin restocks the variant to `stock > 0` (see `PUT /api/v1/admin/variants/{id}/stock`).
