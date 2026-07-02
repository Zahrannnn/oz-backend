# Product Requirements Document (PRD)

# School Uniform Store Platform

**Version:** 1.2  
**Product Owner:** Store Owner  
**Technical Lead:** Mohamed Zahran  
**Status:** Draft  
**Target Release:** Version 1 (MVP)  
**Last Updated:** July 2026

---

# Table of Contents

1. Product Vision
2. Product Goals
3. Success Metrics
4. Stakeholders
5. Target Users
6. User Personas
7. Product Scope
8. Functional Requirements
9. Non-Functional Requirements
10. User Stories
11. User Flows
12. Business Rules
13. Edge Cases
14. Product Constraints
15. Release Plan
16. Future Roadmap
17. Open Questions
18. Appendix

---

# 1. Product Vision

The School Uniform Store Platform is a modern e-commerce platform built specifically for school uniform retailers.

The platform enables parents to quickly find school uniforms by selecting a school and grade, complete purchases online using Cash on Delivery, and track their orders without creating an account.

The administration portal enables the business owner to efficiently manage products, inventory, deliveries, exchanges, and reporting from a centralized dashboard.

The architecture is intentionally designed for future expansion to support:

- Parent accounts
- POS integration
- Online payments
- Multiple branches
- Multiple cities
- Loyalty programs
- Mobile applications

---

# 2. Product Goals

## Business Goals

- Increase online sales.
- Reduce manual inventory management.
- Improve operational efficiency.
- Improve inventory accuracy.
- Reduce customer support requests.
- Digitize the entire ordering process.

---

## Customer Goals

Parents should be able to:

- Find uniforms quickly.
- Purchase with minimal effort.
- Track orders.
- Receive order updates.
- Know product availability.

---

## Admin Goals

The administrator should be able to:

- Manage products.
- Manage schools.
- Manage inventory.
- Process customer orders.
- Manage exchanges.
- View business reports.

---

# 3. Success Metrics

## Business KPIs

- Online Revenue
- Number of Orders
- Average Order Value
- Monthly Sales
- Inventory Accuracy
- Delivery Success Rate

---

## Customer KPIs

- Checkout Completion Rate
- Customer Satisfaction
- Order Tracking Usage
- Notify Me Conversion Rate

---

## Operational KPIs

- Order Processing Time
- Exchange Processing Time
- Inventory Accuracy
- Email Delivery Success

---

# 4. Stakeholders

## Business

- Store Owner

---

## Internal

- Technical Lead
- Frontend Team
- Backend Team
- UI/UX Designer
- QA Engineer

---

## External

- Parents
- Bosta
- SMTP Provider

---

# 5. Target Users

## Parent

Responsibilities

- Browse uniforms
- Purchase products
- Track orders
- Request stock notifications

Pain Points

- Doesn't know available sizes.
- Doesn't know current stock.
- Doesn't want to visit the store unnecessarily.

---

## Administrator

Responsibilities

- Manage products
- Manage inventory
- Process orders
- Process exchanges
- Generate reports

Pain Points

- Manual inventory
- Paper records
- Manual order management

---

# 6. User Personas

## Persona 1

### Ahmed

Occupation

Employee

Goals

- Purchase uniforms quickly.
- Receive home delivery.
- Track order status.

Frustrations

- Long store visits.
- Phone ordering.
- Unknown stock availability.

---

## Persona 2

### Store Owner

Goals

- Reduce manual work.
- Increase sales.
- Improve reporting.
- Keep inventory accurate.

---

# 7. Product Scope

## Customer Storefront

### Landing Page

Features

- Featured Schools
- Search
- Categories
- Featured Products

---

### School Selection

Features

- Browse Schools
- Search Schools

---

### Grade Selection

Features

- Primary
- Preparatory
- Secondary

---

### Product Listing

Features

- Filters
- Search
- Product Cards
- Stock Status
- Price
- Images

---

### Product Details

Features

- Gallery
- Product Information
- Size Selection
- Stock Status
- Add to Cart
- Notify Me
- Add Full Set

---

### Shopping Cart

Features

- Quantity Update
- Remove Items
- Cart Total
- VAT Included

---

### Checkout

Features

- Customer Information
- Address
- Delivery Method
- Order Summary
- Place Order

---

### Order Tracking

Features

- Timeline
- Status
- Tracking Number

---

## Administration Portal

### Dashboard

Widgets

- Revenue
- Orders
- Low Stock
- Pending Orders
- Notify Me
- Recent Activity

---

### Schools

CRUD

- Create
- Update
- Delete
- Search

---

### Products

CRUD

- Create
- Edit
- Archive
- Images
- Variants

---

### Inventory

Features

- Stock Adjustment
- Stock History
- Low Stock Alerts

---

### Orders

Features

- Search
- Filter
- View Details
- Update Status
- Cancel
- Print Invoice

---

### Exchanges

Features

- Partial Exchange
- Size Change
- Item Replacement
- Price Difference
- Inventory Adjustment

---

### Reports

Reports

- Sales
- Inventory
- Orders
- Notify Me

---

# 8. Functional Requirements

## FR-001 Browse Schools

Priority

High

Description

Parents can browse available schools.

Acceptance Criteria

- Schools displayed successfully.
- Search works.
- Selecting a school navigates to grades.

---

## FR-002 Browse Products

Priority

High

Acceptance Criteria

- Products filtered by school.
- Products filtered by grade.
- Products filtered by gender.

---

## FR-003 Product Details

Acceptance Criteria

- Gallery visible.
- Size selection required.
- Out-of-stock status displayed.
- Product price visible.

---

## FR-004 Shopping Cart

Acceptance Criteria

- Add item.
- Remove item.
- Update quantity.
- Calculate totals.

---

## FR-005 Checkout

Acceptance Criteria

- Customer information validated.
- Delivery method selected.
- Order created.
- Confirmation email sent.

---

## FR-006 Inventory

Acceptance Criteria

- Stock deducted after successful order (atomic check + decrement inside the place-order transaction; no reservation layer).
- Prevent overselling.
- Restore stock after cancellation.
- Manual stock edit: administrator types a new stock value per variant; the system computes the diff.
- Diff-based notify-me trigger: when saved stock crosses 0 → positive, notify-me emails for that variant are fired.
- Diff-based low-stock trigger: when saved stock falls below the per-variant threshold, the variant appears in the admin dashboard low-stock widget (no email to the administrator).
- Low-stock threshold configurable per variant.

---

## FR-007 Notify Me

Acceptance Criteria

- Email captured on out-of-stock product detail.
- Duplicate requests per (variant, email) prevented.
- Email sent after restock (triggered by the diff-based stock save described in FR-006).
- Notify-me is a standalone flow; notify-me is **not** an order mechanism (out-of-stock items are not orderable).

---

## FR-008 Order Tracking

Acceptance Criteria

- Per-order unguessable token URL issued at checkout; this is the parent's sole access path to the order status page (no order-ID lookup, no SMS, no WhatsApp, no push).
- The token URL is included in every order-update email (placed, shipped, delivered, cod_failed).
- Timeline visible.
- Current order status displayed.

---

## FR-009 Admin Authentication

Acceptance Criteria

- Single-owner email + password login.
- Password stored as a hash (bcrypt or argon2).
- Session via a single JWT in an httpOnly cookie.
- 8-hour token expiry, sliding renewal on every admin action. No refresh-token rotation in v1.
- On first successful login, the system displays a one-time recovery code once and prompts the owner to save it. The recovery code (plus the recovery email) is the only password-reset path; no email-based reset link in v1.
- Every successful login and every failed-login attempt is written to the audit log (see FR-016).

---

## FR-010 Product Management

Acceptance Criteria

- CRUD operations for products.
- Each product = (school + grade-stage + item-type + gender). Color is an admin-fixed product attribute, not a parent-selectable variant.
- Multiple images per product (gallery; sorted by `sort_order`). Stored on the backend host (`MonsterASP` local storage) under `/uploads`. No CDN.
- Variants per product: size (free-text label, admin-owned) + per-variant price (EGP, VAT-inclusive) + per-variant stock count + per-variant low-stock threshold.
- Boolean `is_in_set` flag: when true, the product is part of the (school + grade + gender) full-set; the storefront exposes an "add full set" button that expands to that set's products at their individual prices (no bundle SKU, no bundle discount).
- Item-types are shared across schools ("trousers" reused); school linkage happens at the product level.

---

## FR-011 Exchange Management

Acceptance Criteria

- In-store exchange only. Parent must physically visit the store; no couriered exchange.
- Admin-logged (parent has no exchange button on the order link).
- Partial exchange supported (parent can swap 1 of N items).
- Original `order_item` rows preserved; exchange rows reference the original line and store old → new + price delta.
- Stock moves on exchange: decrement new item/size, increment returned item/size.
- Order total recomputes from the effective line items (no separate cash-delta record; in-store cash settlement).

---

## FR-012 Reporting

Acceptance Criteria

- Sales summary: orders count, revenue, by day/week/month, by school, by channel (delivery vs pickup).
- Inventory status: current stock per variant + low-stock list.
- Order list: filterable by status, school, date, courier status; includes `cod_failed` / `returned` tracking.
- Notify-me demand: which out-of-stock variants have the most parent requests, to prioritize restocking.

---

## FR-013 Order State Machine

Acceptance Criteria

- Order moves through the following states:
  - Home delivery: `placed → ready_to_ship → handed_to_courier → in_transit → delivered → closed_success`
  - Home delivery failure: `in_transit → cod_failed → returned_to_store → closed_failed`
  - Store pickup: `ready_to_ship → ready_for_pickup → picked_up → closed_success`
  - Cancellation: parent may cancel from `placed` or `ready_to_ship` (delivery) or `placed`/`ready_to_ship`/`ready_for_pickup` (pickup). No cancellation after `handed_to_courier` or `picked_up`.
- Cancellation refunds stock atomically.
- Auto-cancel + restock: a Hangfire scheduled job cancels any order in a pre-handoff state (`placed`, `ready_to_ship`, `ready_for_pickup`) that has been idle for more than 5 days, and refunds stock. Does not apply to `handed_to_courier` / `in_transit` (in Bosta's hands).
- Out-of-stock items are not orderable, so no `awaiting_stock` state exists.

---

## FR-014 Catalog Browsing

Acceptance Criteria

- Homepage exposes both a school list and a school-name search.
- School-first funnel: pick school → pick grade-stage (explicit step) → product grid.
- Grade-stage chips: Ebtda2y / E3dady / Sanawy; some schools have only one grade-stage enabled.
- Product grid supports filters (item type, gender) and shows stock status + price.
- "Add full set" button on the (school + grade + gender) view expands to all set-membership products at their individual per-variant prices; no bundle SKU, no bundle discount.
- School-name search uses **MiniSearch** (JS fuzzy-search library) running client-side on the Next.js frontend. With ~30 schools in the catalog, the entire list ships in the initial payload and search is instant, with no backend roundtrip. The backend still exposes a `LIKE '%query%'` server-side filter on the schools list endpoint for any non-search programmatic use.

---

## FR-015 Store-Pickup Counter Flow

Acceptance Criteria

- Order in `ready_for_pickup` state is identifiable at the counter by the parent's phone number or the order number, both captured at checkout.
- Admin backend supports a phone-number lookup that lists matching `ready_for_pickup` orders for the day, and an order-id direct lookup as a fallback.
- Admin opens the order, reviews the line items, hands them over, and clicks "mark picked up" which transitions the order to `picked_up` → `closed_success` and writes an audit log entry.
- The per-order token URL is accepted as a backup identification method: scanning / opening the URL opens the order directly in the admin view.
- Stock is **not** decremented again at pickup (it was already decremented at `placed`, per Q19).

---

## FR-016 Admin Audit Log

Acceptance Criteria

- State-changing actions are recorded: actor (admin id), timestamp, action type, target entity id, before-value, after-value, optional reason field.
- Specifically logged: order status transitions, manual stock edits (stock-in / stock-out adjustments), exchange log entries, CRUD on products, schools, item-types, grade-stages, variants, and product images, and admin login success / failure.
- Read-only views (reports, dashboards, order browsing) are not logged.
- Audit log is visible in the admin backend (filterable by actor, action, date, target) and exportable as CSV.

# 9. Non-Functional Requirements

## Performance

- Initial page load < 2 seconds
- Checkout < 3 seconds
- Dashboard < 2 seconds

---

## Availability

Target Uptime

99.5%

---

## Scalability

Support

- 10,000 Products
- 100,000 Variants
- Seasonal traffic
- Future POS integration

---

## Security

- HTTPS
- JWT Authentication
- Refresh Tokens
- Security Headers
- Rate Limiting
- Audit Logs
- Input Validation

---

## Accessibility

- Responsive Design
- RTL Support
- Keyboard Navigation
- Screen Reader Compatibility

---

# 10. User Stories

### US-001

As a parent

I want to browse schools

So I can find my child's school.

---

### US-002

As a parent

I want to search schools

So I can quickly find my school.

---

### US-003

As a parent

I want to browse uniforms

So I can purchase them.

---

### US-004

As a parent

I want to add products to my cart

So I can buy everything together.

---

### US-005

As a parent

I want to complete checkout

So I receive my order.

---

### US-006

As a parent

I want to track my order

So I know its delivery status.

---

### US-007

As an administrator

I want to manage inventory

So stock remains accurate.

---

### US-008

As an administrator

I want to process customer orders

So deliveries happen on time.

---

# 11. User Flows

## Customer Journey

```text
Landing

↓

Select School

↓

Select Grade

↓

Browse Products

↓

Product Details

↓

Shopping Cart

↓

Checkout

↓

Order Confirmation

↓

Order Tracking
```

---

## Admin Journey

```text
Login

↓

Dashboard

↓

Orders

↓

Update Status

↓

Inventory

↓

Reports
```

---

# 12. Business Rules

- Guest checkout only.
- Cash on Delivery only.
- One city supported.
- One administrator (single owner; email + password login).
- No refunds.
- In-store exchanges only (parent must physically visit the store; admin-logged; no parent self-service exchange button).
- VAT (14%) included in displayed prices.
- Inventory deducted immediately after successful order (atomic, inside the place-order transaction).
- Cancelled orders restore inventory.
- Auto-cancel + restock: pre-handoff orders idle for more than 5 days are cancelled by the Hangfire scheduled job and stock is refunded.
- Schools are catalog filters only (no contracts, no bulk school orders in v1).
- Products cannot be purchased when out of stock.
- Color is admin-fixed per (school + grade + item) and is not a parent-selectable variant.
- Size labels are free-text per variant; no size-scale templates.
- "Add full set" is a storefront convenience backed by `product.is_in_set = true`; no priced bundle, no bundle SKU, no bundle discount.
- Item-types are shared across schools; school linkage happens at the product level.
- Notify-me captures parent email on out-of-stock products; emails are sent on the diff-based restock trigger. Notify-me is **not** an order mechanism.
- Order status is reached via a per-order unguessable token URL emailed to the parent; no order-ID lookup.
- Low-stock alerts surface on the admin dashboard widget only; no email alerts to the administrator.
- Bosta pickup is booked per order; no batch pickup action in v1.
- Store-pickup counter flow: parent provides phone or order number at the counter; admin opens the order by phone lookup or order id, reviews, and clicks "mark picked up". Token URL is the backup identification if phone / order number is missing. Stock is not decremented again at pickup (already done at `placed`).
- Admin audit log: state-changing actions only (order status transitions, manual stock edits, exchange log entries, CRUD on products / schools / item-types / grade-stages / variants / product images, admin login success and failure). Read-only views are not logged.
- Admin auth: single JWT, 8-hour sliding expiry, no refresh-token rotation in v1. Password recovery is by one-time recovery code shown at first admin login.

---

# 13. Edge Cases

Customer

- Invalid tracking token.
- Product removed.
- Product becomes unavailable during checkout (OOS detected at place-time → atomic check rejects the order).
- Email sending failure (retried by the email transport; status page still reachable via the token URL).
- Duplicate Notify Me request.
- Cart lost on device/browser clear (localStorage; intentional).

Administrator

- Inventory reaches zero (admin can still edit; OOS variants hide "add to cart" and show "notify me").
- Exchange with price difference (in-store cash settlement; order total recomputes).
- Failed Bosta API request (admin retries per order; no batch action in v1).
- Failed email queue (Hangfire-style retry; order status still updates server-side).
- Duplicate inventory update (last-write-wins on the saved value; diff-based triggers re-evaluate on each save).
- Stalled pre-handoff order: Hangfire auto-cancels at 5 days, refunds stock, and (if email transport is healthy) notifies the parent.

---

# 14. Product Constraints

- Arabic-only UI (RTL). Data names (schools, items, grade-stages) are stored in a single freeform text field; the administrator enters whatever script they use. No bilingual columns.
- Guest checkout
- Cash on Delivery
- One city
- One physical store
- Bosta delivery only (`ShippingProvider` interface reserved for future providers)
- Email notifications only (SMTP)
- No SMS, no WhatsApp, no push notifications to parents
- Local image storage on the backend host (`MonsterASP`); no CDN
- Microsoft SQL Server database
- Backend: separate .NET project hosted on `MonsterASP` (REST API, business logic, DB access, Bosta integration, email transport, Hangfire scheduler, admin auth)
- Frontend: Next.js 16 (React 19) on Vercel — pure React UI, no Next.js API routes
- Hybrid data fetching: catalog pages (schools, grades, products) use SSR + ISR for SEO; cart, checkout, order tracking, and admin are client-side
- CORS locked to the Vercel origin; admin JWT in an httpOnly cookie
- Email transport: **Gmail SMTP** (locked). Free tier caps at 500 emails/day, Workspace at 2,000/day. Sufficient for launch volume; revisit if the store scales past these limits.
- School-name search: **MiniSearch** (JS) on the Next.js frontend, with a MSSQL `LIKE` server-side filter on the schools list endpoint as a fallback.
- Admin auth: single JWT in an httpOnly cookie, 8-hour sliding expiry, no refresh-token rotation in v1.

---

# 15. Release Plan

## Phase 1

Customer Storefront

- Landing
- Schools
- Products
- Cart

---

## Phase 2

Ordering

- Checkout
- Orders
- Tracking
- Email
- Bosta

---

## Phase 3

Administration

- Dashboard
- Inventory
- Orders
- Reports

---

## Phase 4

Operational Features

- Exchanges
- Notify Me
- Hangfire Jobs
- Performance Optimization

---

# 16. Future Roadmap

## Version 2

- Parent Accounts
- Online Payments
- Coupons
- Loyalty Program
- POS Integration
- Multiple Delivery Providers

---

## Version 3

- Mobile Applications
- Multiple Stores
- Multiple Cities
- Supplier Management
- Purchase Orders
- AI Demand Forecasting

---

# 17. Open Questions

All product and product-adjacent open questions from the interview are now locked (see Appendix E for the full set of decisions made in this document version). The remaining open items are operations / vendor setup, not product decisions:

- Bosta account + API credentials + sandbox-vs-production plan (operations; blocks cutover).
- MonsterASP SQL Server tier + connection-string plumbing to the .NET project (operations; blocks deployment).
- Email sender domain + SPF/DKIM/DMARC setup (operations; affects deliverability of the Gmail SMTP transport).
- Bosta pickup slot window: confirm whether Bosta gives a flexible pickup window (today / tomorrow / N hours) or a fixed slot (operations; affects admin order-handling UX).
- POS timeline (long-term roadmap, not v1).
- Phased vs one-shot build rollout: deferred until implementation begins; current focus is docs only.

---


---

# Appendix A - Feature Prioritization (MoSCoW)

## Must Have

- Product Catalog
- Inventory Management
- Guest Checkout
- Cash on Delivery
- Order Tracking
- Email Notifications
- Admin Dashboard
- Reporting
- Exchange Management

---

## Should Have

- Notify Me
- Product Search
- Full Uniform Set

---

## Could Have

- Advanced Analytics
- Delivery Dashboard
- Seasonal Reports

---

## Won't Have (V1)

- Parent Accounts
- Online Payments
- Refunds
- POS
- Coupons
- Loyalty Program
- Mobile Application
- SMS Notifications
- WhatsApp Notifications
- Multi-Store
- Multi-City
- Priced product bundles / set discounts
- Size guide / size charts
- Bilingual (name_ar / name_en) data columns
- CSV / Excel catalog import
- Batch Bosta pickup booking
- Customer self-service exchange button
- Order-ID lookup on the storefront (parent uses the token URL only)

---

# Appendix B - Order State Machine (Locked)

```
placed → ready_to_ship → handed_to_courier → in_transit → delivered → closed_success
                                        in_transit → cod_failed → returned_to_store → closed_failed

placed → ready_to_ship → ready_for_pickup → picked_up → closed_success

placed / ready_to_ship  → cancelled          (delivery: until handed_to_courier)
placed / ready_to_ship / ready_for_pickup → cancelled   (pickup: until picked_up)
```

Notes:
- OOS items are not orderable → no `awaiting_stock` state exists.
- Cancellation refunds stock atomically.
- Auto-cancel: any pre-handoff state (`placed`, `ready_to_ship`, `ready_for_pickup`) idle for more than 5 days is cancelled and restocked by the Hangfire scheduled job. Does not apply to `handed_to_courier` / `in_transit` (in Bosta's hands).
- `not_picked_up` does not apply to `handed_to_courier` / `in_transit` (in Bosta's hands).
- `PaymentProvider` and `ShippingProvider` interfaces reserved for v2 (Paymob, additional couriers).
- Per-order Bosta pickup booking only; no batch action in v1.

---

# Appendix C - Domain Model Notes (Interview-Locked)

- **Product** = (school + grade-stage + item-type + gender). Grade-stages: Ebtda2y / E3dady / Sanawy; some schools single-grade.
- **Variant** = size only (free-text). Per-variant price (EGP, VAT-inclusive), per-variant stock count, per-variant low-stock threshold.
- **Color** is an admin-fixed product attribute, not a parent-selectable variant.
- **Image**: multi-image gallery per product (`product_image`: product_id, url, sort_order). Served from backend `/uploads`. No CDN.
- **Set**: `product.is_in_set` boolean. "Add full set" expands to the (school + grade + gender) set membership at individual prices.
- **Order**: `placed` decrements stock atomically; cancel refunds. Token URL issued at checkout.
- **Exchange**: in-store, admin-logged, partial. `order_item` rows preserved; `exchange` rows reference them and store old → new + price delta. Order total recomputes.
- **Item-types**: shared across schools. School linkage happens at the product level.
- **Sizes**: free-text per variant. Admin owns the strings.
- **Notify-me**: standalone flow on out-of-stock products. Email captured, restock diff-based trigger fires email.

---

# Appendix D - Version History

| Version | Date | Author | Description |
|----------|------|--------|-------------|
| 1.0 | July 2026 | Mohamed Zahran | Initial PRD |
| 1.1 | July 2026 | Mohamed Zahran | Aligned with product-owner interview Q1–Q31: added 5-day auto-cancel FR-013, explicit order state machine (Appendix B), domain model notes (Appendix C), color-as-attribute, token-link order access, manual stock + diff trigger, add-full-set semantics, technical constraints (MS SQL + .NET + MonsterASP + Vercel + hybrid fetch), updated out-of-scope list, flagged JWT/refresh + scale numbers as open questions |
| 1.2 | July 2026 | Mohamed Zahran | Locked remaining open items: password recovery = one-time code at first login (FR-009); store-pickup counter flow = phone or order number lookup, admin "mark picked up", token URL backup (FR-015); 10K products / 100K variants scale targets confirmed; email transport = Gmail SMTP (Constraint); school search = MiniSearch on frontend + MSSQL LIKE on backend (FR-014); admin auth = single JWT, 8h sliding expiry, no refresh token (FR-009); audit log = state-changing actions only (FR-016). Open Questions collapsed to ops-only items. |

---

# Appendix E - Locked Decisions (Session Log)

Decisions locked in the v1.2 review session, cross-referenced to where they are recorded in this PRD:

| # | Decision | Where |
|---|----------|-------|
| 1 | Password recovery = one-time recovery code shown at first admin login (no email reset link in v1) | FR-009, Business Rules |
| 2 | Store-pickup counter: parent gives phone or order number; admin opens order by phone lookup or order id, clicks "mark picked up"; token URL is a backup id | FR-015, Business Rules |
| 3 | Build is docs-only for now; phased-vs-one-shot build decision is deferred to implementation time | Open Questions |
| 4 | Scalability target = 10,000 products / 100,000 variants (confirmed by product owner) | NFR Scalability, BRD Assumptions |
| 5 | Email transport = Gmail SMTP (known daily-cap limitation accepted) | Constraints, Assumptions |
| 6 | School search = MiniSearch on Next.js frontend + MSSQL `LIKE` on backend | FR-014, Constraints |
| 7 | Admin auth = single JWT in httpOnly cookie, 8-hour sliding expiry, no refresh-token rotation in v1 | FR-009, Business Rules |
| 8 | Audit log = state-changing actions only (order transitions, stock edits, exchange logs, CRUD on catalog entities, admin login success/failure) | FR-016, Business Rules |