# Use Case Diagram

# School Uniform Store Platform

**Version:** 1.0
**Author:** Mohamed Zahran
**Status:** Draft (aligned with PRD v1.2 + SDD v1.0)
**Date:** July 2026

---

## Files

| Format | File |
|--------|------|
| PlantUML source | `use-case-diagram.puml` |
| SVG diagram | `use-case-diagram.svg` |

---

## Actors

| Actor | Description |
|-------|-------------|
| **Parent** | Guest customer; browses catalog, places orders, tracks via token URL |
| **Administrator** | Single store owner; manages catalog, inventory, orders, exchanges |
| **Bosta** | External courier system; receives pickup bookings, sends tracking webhooks |
| **SMTP Server** | Gmail SMTP; delivers order-update and notify-me emails |

---

## Actor-Use Case Mapping

### Parent (guest customer)

| # | Use Case | Trigger | Result |
|---|----------|---------|--------|
| P1 | Browse Schools | Landing page load | School list from ISR |
| P2 | Search Schools | Typing in search box | MiniSearch fuzzy results |
| P3 | Browse Products by Grade | School + grade selected | Product grid w/ stock status |
| P4 | View Product Details | Product card clicked | Gallery, variants, price |
| P5 | Manage Cart | Add/remove/update items | localStorage cart |
| P6 | Add Full Set to Cart | "Add full set" button clicked | Set expanded to line items |
| P7 | Place Order | Checkout form submitted | Order created, token issued, email enqueued |
| P8 | Track Order | Token URL visited | Timeline + current status |
| P9 | Cancel Order | Cancel link on status page | Pre-handoff → cancelled, stock refunded |
| P10 | Request Notify-Me | Email entered on OOS product | Request stored, email fired on restock |

### Administrator

| # | Use Case | Trigger | Result |
|---|----------|---------|--------|
| A1 | Login | Admin visits `/admin` | JWT cookie set |
| A2 | Manage Schools | Schools CRUD screen | School rows created/updated/archived |
| A3 | Manage Grade Stages | Grade-stages screen per school | Grade-stage rows |
| A4 | Manage Item Types | Item-types screen | Shared item-type rows |
| A5 | Manage Products | Products CRUD screen | Product rows + set flag |
| A6 | Manage Variants | Variants screen per product | Variant rows (size, price, threshold) |
| A7 | Manage Product Images | Image upload per product | Files on MonsterASP, rows in `product_image` |
| A8 | Update Stock | Stock edit form | Manual edit + diff triggers (notify-me, low-stock) |
| A9 | View Dashboard | Dashboard load | KPI tiles + low-stock widget |
| A10 | Process Orders | Orders list → detail | State transitions (whitelisted) |
| A11 | Book Bosta Pickup | Order action button | Shipment created via Bosta API, state → `handed_to_courier` |
| A12 | Mark Picked Up | Counter order lookup (phone/order#) | State → `picked_up` → `closed_success` |
| A13 | Log Exchange | Exchange form in order | Stock traded, price delta computed, total updated |
| A14 | View Reports | Reports menu | Sales, inventory, orders, notify-me |
| A15 | Browse Audit Log | Audit log screen | Filterable action history |
| A16 | Export Audit Log | Export button | CSV download |

### Bosta (external system)

| # | Use Case | Direction | Result |
|---|----------|-----------|--------|
| B1 | Create Shipment | System → Bosta | Pickup booked, tracking ID stored |
| B2 | Send Tracking Webhook | Bosta → System | Order state updated (delivered, COD-failed, returned) |

### SMTP (external system)

| # | Use Case | Direction | Result |
|---|----------|-----------|--------|
| S1 | Send Email | System → SMTP | Order-update or notify-me email delivered |

---

## Key System Interactions

- **Place Order** (P7) runs inside a single DB transaction: lock variant rows → check stock → deduct inventory → insert `order` + `order_item` → generate token → enqueue confirmation email via Hangfire. If stock is insufficient, the entire transaction rolls back and the parent receives `409 out-of-stock`.
- **Process Orders** (A10) delegates to state-specific sub-actions: transition to next state (whitelist-guarded), cancel, book Bosta pickup, mark picked up at the counter.
- **Update Stock** (A8) triggers two side effects outside the transaction: (1) if stock crosses 0→positive, notify-me emails fire for all pending requests on that variant; (2) if stock falls below the per-variant threshold, the dashboard low-stock widget flags it.
- **Log Exchange** (A13) is a multi-step transaction: refund returned variant stock → take new variant stock → compute price delta → update order total → write audit log.
- The **5-day auto-cancel** is a scheduled system behavior (Hangfire recurring job), not mapped as a use case. It scans pre-handoff orders daily and cancels + refunds any stalled beyond 5 days.
- **Order-update emails** (confirmation, shipped, delivered, COD-failed, cancelled) are triggered by state transitions, not by parent action. Notify-me emails are triggered by the stock-edit diff. All emails use the parent email captured at checkout.
