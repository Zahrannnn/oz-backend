# Sequence Diagrams

# School Uniform Store Platform

**Version:** 1.0
**Author:** Mohamed Zahran
**Status:** Aligned with SDD v1.0 + PRD v1.2
**Date:** July 2026

---

## Diagram Index

| # | File | What it Covers | SVG |
|---|------|----------------|------|
| 01 | `sd-01-place-order.puml` | Checkout → atomic UPDLOCK tx → 201 → Hangfire email job → confirmation | `sd-01-place-order.svg` (29 KB) |
| 02 | `sd-02-bosta-delivery.puml` | Admin books pickup → Bosta API → 3 webhook outcomes (delivered/COD-failed/returned w/ stock refund) | `sd-02-bosta-delivery.svg` (36 KB) |
| 03 | `sd-03-cancel-order.puml` | Token cancel → state guard (pre-handoff only) → atomic stock restore → email → 409 on invalid state | `sd-03-cancel-order.svg` (22 KB) |
| 04 | `sd-04-exchange.puml` | Multi-step tx: refund old stock → take new → price delta → update total → cash settlement | `sd-04-exchange.svg` (26 KB) |
| 05 | `sd-05-auto-cancel.puml` | Hangfire daily job → select stale orders → loop atomic cancel+stock+email per order | `sd-05-auto-cancel.svg` (22 KB) |
| 06 | `sd-06-notify-me-restock.puml` | Stock edit → detect zero-cross → batch query pending_alerts → notify emails | `sd-06-notify-me-restock.svg` (23 KB) |
| 07 | `sd-07-browse-catalog.puml` | SSR+ISR hybrid fetch: ISR schools list → API grades+products+variants (N+1 noted) | `sd-07-browse-catalog.svg` (32 KB) |
| 08 | `sd-08-mark-picked-up.puml` | Counter: parent gives phone → admin searches → verify → picked_up → closed_success | `sd-08-mark-picked-up.svg` (30 KB) |
| 09 | `sd-09-admin-login.puml` | JWT auth: credentials → failed_attempts lockout → cookie Set-Cookie with 8h sliding | `sd-09-admin-login.svg` (26 KB) |
| 10 | `sd-10-request-notify-me.puml` | Parent email on OOS → store pending_alert → guard (in-stock / dup) → audit | `sd-10-request-notify-me.svg` (23 KB) |
| 11 | `sd-11-view-order-by-token.puml` | Token URL → SHA2-256 hash → DB lookup → status timeline → hide/show cancel | `sd-11-view-order-by-token.svg` (23 KB) |
| 12 | `sd-12-admin-password-recovery.puml` | One-time code: forgot password → 6-digit code → code_hash verify → JWT cookie | `sd-12-admin-password-recovery.svg` (34 KB) |

---

## Coverage Matrix

| Actor | Flows Covered |
|-------|---------------|
| **Parent** | place order (01), cancel (03), browse catalog (07), view order by token (11), request notify-me (10), picked up at counter (08) |
| **Administrator** | book bosta pickup (02), log exchange (04), update stock + notify-me trigger (06), mark picked up (08), login (09), password recovery (12) |
| **Bosta** | create shipment (02), webhook: delivered/COD-failed/returned (02) |
| **SMTP** | order confirmation (01), shipped (02), delivery confirmation (02), COD failure (02), cancellation (03), auto-cancel (05), notify-me restock (06), password recovery usage (12) |
| **System** | auto-cancel job (05), audit log write (all) |

## Architectural Patterns Visible Across Diagrams

1. **Atomic transactions with row-level locking**: Place order `WITH (UPDLOCK, ROWLOCK)` prevents concurrent oversell. Exchange, cancel, auto-cancel all wrap mutations in single tx.

2. **State machine guards**: Cancel checks pre-handoff states. Bosta webhook validates `in_transit` before processing. Pickup requires `ready_for_pickup`. Each transition only accepts the correct previous state.

3. **Token-based parent access**: Orders identified by SHA2-256 hash of URL token — no login required. Cancel, view, and all timeline operations use this pattern.

4. **HMAC-verified webhooks**: Bosta callback verified via SHA256 shared secret before order state mutations.

5. **Hangfire async jobs**: Email (confirmation, shipping, cancel, auto-cancel, notify-me) deferred to Hangfire queue. Never blocks API response.

6. **Audit log at every mutation**: Every diagram writes `audit_log` row (action, actor, order_id, metadata). No read-only views logged.

7. **Idempotent side effects**: Notify-me checks `notified=false`. Auto-cancel skips already-cancelled. Webhook validates state.

8. **No email enumeration**: Password recovery always returns 200, even if email not found.
