# Business Requirements Document (BRD)

# School Uniform Store Platform

**Version:** 1.2  
**Project Sponsor:** Store Owner  
**Document Owner:** Mohamed Zahran (Technical Lead)  
**Status:** Draft  
**Project Type:** Web Application  
**Last Updated:** July 2026

---

# Table of Contents

1. Executive Summary
2. Business Background
3. Business Objectives
4. Business Goals
5. Stakeholders
6. Business Scope
7. Current Business Process
8. Proposed Business Process
9. Business Requirements
10. Business Rules
11. Assumptions
12. Constraints
13. Risks
14. Success Criteria
15. Future Business Roadmap
16. Out of Scope
17. Appendix

---

# 1. Executive Summary

The School Uniform Store Platform is a digital transformation initiative aimed at modernizing the operations of a school uniform retailer operating within a single city in Egypt.

The platform replaces manual inventory management, paper-based order processing, and phone-based customer communication with a centralized online ordering system and administration portal.

Version 1 focuses on digitizing the current business workflow without changing existing operational policies. The solution is designed with future scalability in mind, enabling expansion to multiple branches, POS integration, online payments, loyalty programs, and mobile applications.

---

# 2. Business Background

The business currently operates through a physical retail store serving multiple schools.

Current operations rely heavily on manual processes including:

- Inventory tracking
- Customer communication
- Order processing
- Delivery coordination
- Reporting

These processes increase operational costs, consume significant staff time, and make inventory visibility difficult.

The objective of this project is to digitize these operations while maintaining the existing business workflow.

---

# 3. Business Objectives

The project aims to achieve the following objectives:

- Digitize the sales process.
- Improve customer experience.
- Reduce manual inventory work.
- Improve inventory accuracy.
- Increase online sales.
- Simplify order management.
- Improve reporting.
- Enable future business growth.

---

# 4. Business Goals

## Short-Term Goals (Version 1)

- Launch an online storefront.
- Digitize inventory management.
- Support online ordering.
- Integrate with Bosta.
- Support Cash on Delivery.
- Reduce manual work.
- Provide operational reports.

---

## Long-Term Goals

- POS Integration
- Online Payments
- Parent Accounts
- Multiple Branches
- Multiple Cities
- Loyalty Program
- Mobile Applications
- AI-powered Demand Forecasting

---

# 5. Stakeholders

## Business Stakeholders

- Store Owner
- Store Employees

---

## Technical Stakeholders

- Technical Lead
- Frontend Team
- Backend Team
- UI/UX Designer
- QA Engineer

---

## External Stakeholders

- Parents
- Delivery Provider (Bosta)
- SMTP Provider

---

# 6. Business Scope

## Customer Features

- Browse Schools
- Browse Products
- Search Products
- Product Details
- Shopping Cart
- Checkout
- Home Delivery
- Store Pickup
- Order Tracking
- Notify Me

---

## Administration Features

- Dashboard
- Product Management
- School Management
- Inventory Management
- Order Management
- Exchange Management
- Reports

---

## Integrations

- Bosta
- Email Notifications

---

# 7. Current Business Process

Current workflow consists of:

1. Customer visits the physical store.
2. Staff checks inventory manually.
3. Customer selects products.
4. Staff records the order manually.
5. Delivery is arranged manually.
6. Reports are prepared manually.

### Current Challenges

- Manual stock tracking.
- Limited inventory visibility.
- Time-consuming order processing.
- Paper-based records.
- Difficult reporting.
- High dependency on staff.

---

# 8. Proposed Business Process

Future workflow:

1. Parent visits the website.
2. Selects school.
3. Selects grade.
4. Browses uniforms.
5. Adds products to cart.
6. Completes checkout.
7. Order is processed automatically.
8. Inventory updates automatically.
9. Delivery request is created.
10. Customer receives email updates.
11. Administrator monitors the order from the dashboard.

---

# 9. Business Requirements

## BR-001 Product Catalog

The platform shall allow customers to browse uniforms by:

- School
- Grade
- Gender
- Item Type

---

## BR-002 Product Variants

Each product shall support:

- Multiple sizes
- Individual prices
- Individual inventory

Color is **not** a parent-selectable variant. Color is fixed per (school + grade + item) spec and stored as a product attribute set by the administrator.

---

## BR-003 Inventory Management

The platform shall:

- Track inventory per variant.
- Prevent overselling.
- Deduct inventory immediately after successful order placement (atomic check + decrement inside the place-order transaction).
- Restore inventory after order cancellation.
- Support manual inventory updates by the administrator (diff-based: when saved stock crosses 0 → positive, notify-me emails fire; when saved stock falls below the per-variant threshold, the low-stock dashboard widget flags it).
- Support a configurable low-stock threshold per variant (dashboard widget only, no email alerts to the administrator).

---

## BR-004 Shopping Cart

Customers shall be able to:

- Add products.
- Remove products.
- Update quantities.
- Review order totals.

---

## BR-005 Checkout

The platform shall support:

- Home delivery.
- Store pickup.
- Cash on Delivery.
- Guest checkout.

---

## BR-006 Order Management

The platform shall:

- Create orders.
- Track order status through the following state machine:
  - Home delivery path: `placed → ready_to_ship → handed_to_courier → in_transit → delivered → closed_success`
  - Home delivery failure path: `in_transit → cod_failed → returned_to_store → closed_failed`
  - Store pickup path: `ready_to_ship → ready_for_pickup → picked_up → closed_success`
  - Cancellation: parent may cancel from `placed` or `ready_to_ship` (delivery) or `placed`/`ready_to_ship`/`ready_for_pickup` (pickup). No cancellation after `handed_to_courier` or `picked_up`.
- Auto-cancel any order sitting in a pre-handoff state (`placed`, `ready_to_ship`, `ready_for_pickup`) for more than 5 days. The Hangfire scheduled job flips the state to `cancelled` and refunds stock. Does not apply to `handed_to_courier` / `in_transit` (in Bosta's hands).
- Allow parent cancellation before shipment (delivery: before `handed_to_courier`; pickup: before `picked_up`).
- Record order history.
- Issue a per-order unguessable token at checkout; the token URL is the parent's sole access path to live order status. The token is included in all order-update emails.

---

## BR-007 Delivery

The platform shall integrate with Bosta for:

- Shipment creation.
- Shipment tracking.
- Cash on Delivery.

---

## BR-008 Customer Notifications

Customers shall receive email notifications for:

- Order confirmation.
- Shipping updates.
- Delivery confirmation.
- Notify Me requests.

---

## BR-009 Exchange Management

The platform shall support:

- In-store exchanges.
- Partial exchanges.
- Inventory adjustment.
- Price difference calculation.

---

## BR-010 Reporting

The platform shall provide:

- Sales Reports
- Inventory Reports
- Order Reports
- Notify Me Reports

---

## BR-011 Admin Authentication

The platform shall provide:

- Email + password login for the single administrator.
- JWT-based session, stored in an httpOnly cookie. Single token, 8-hour sliding expiry (renewed on each admin action). No refresh-token rotation in v1.
- One-time recovery code displayed on first login and saved offline by the owner. Recovery code is required to reset a lost password.
- Audit logging on every successful login and every authentication failure (rate-limited and surfaced in the audit log).

---

# 10. Business Rules

- Guest checkout only.
- Cash on Delivery only.
- One administrator.
- One city supported.
- One physical store.
- VAT (14%) included in displayed prices.
- Products cannot be purchased if out of stock.
- Inventory is deducted immediately after order creation (atomic).
- Cancelled orders restore inventory.
- Auto-cancel + restock: any pre-handoff order idle for more than 5 days is cancelled by the Hangfire scheduled job and stock is restored.
- No refunds.
- Exchanges are performed only in-store; parent must physically visit the store (no couriered exchange).
- Exchanges are admin-logged (no parent self-service exchange button).
- Partial exchange supported: original `order_item` rows are preserved; exchange rows reference the original line and store old → new plus price delta. Order total recomputes from the effective line items. Stock moves on exchange (decrement new size, increment returned size). In-store cash settlement of any price delta.
- Schools act as catalog filters only (no contracts, no bulk orders, no school POs in v1).
- Product sizes maintain independent inventory.
- Customers track orders using a per-order secure token URL emailed to them (no order-ID lookup, no SMS, no WhatsApp, no push).
- Order-update emails and notify-me alerts are the only email sends in v1.
- A "full set" is a storefront convenience, not a priced catalog entity. Admin marks `is_in_set = true` on each product that belongs to the (school + grade + gender) set. "Add full set" expands to N line items at individual per-variant prices; no bundle SKU, no bundle discount.
- Color is admin-fixed per (school + grade + item) and is not a parent-selectable variant.
- Low-stock alerts surface only on the admin dashboard widget; no email to the administrator.
- Bosta pickup is booked per order; no batch pickup action in v1.
- **Store-pickup counter flow:** at the counter, the parent provides either a phone number or the order number. The administrator opens the order in the backend by phone lookup (search by parent phone captured at checkout) or by order id, reviews the items, hands them over, and clicks "mark picked up" which transitions the order to `picked_up` → `closed_success`. The per-order token URL is accepted as a backup identification method if the parent cannot recall their phone or order number.
- **Admin password recovery:** on first successful admin login, the system displays a one-time recovery code once and asks the owner to save it offline (e.g., password manager, printed paper in a safe). If the password is lost, the recovery code plus a request to the system (in-app or via the recovery email) resets the password. This is the only self-service recovery path; no email-based reset link in v1.
- **Admin audit log:** state-changing actions are recorded with actor, timestamp, before/after values, and reason. Specifically: order status transitions, stock edits (manual stock-in), exchange log entries, and CRUD operations on product, school, item-type, grade-stage, and variant. Read-only views (reports, dashboards, order browsing) are not logged.

---

# 11. Business Assumptions

The project assumes:

- The business operates from a single location.
- Customers have internet access.
- Customers have email addresses.
- Bosta service remains available.
- Cash on Delivery remains the preferred payment method.
- Inventory updates are performed accurately by the administrator.
- Target scale at peak: ~10,000 products and ~100,000 variants (gives ~10× headroom over a realistic 30-school catalog).
- Admin email transport is **Gmail SMTP** (locked in product-owner session). Known limitation: Gmail free tier caps at 500 emails/day, Workspace at 2,000/day. Sufficient for launch; revisit if the store scales past these limits.
- The single owner is responsible for storing the one-time recovery code shown at first admin login (see BR-009).

---

# 12. Business Constraints

- Arabic language only (RTL).
- Data names (schools, items, grade-stages) are stored in a single freeform text field; the administrator enters whatever script they use. No bilingual columns.
- Guest checkout.
- Cash on Delivery.
- Single administrator (single owner; email + password login).
- Single store.
- Single city.
- Bosta delivery only. `ShippingProvider` interface reserved for future providers.
- Email notifications only (SMTP). No SMS, no WhatsApp, no push.
- Local image storage on the backend host (`MonsterASP`); multi-image gallery per product; no CDN.
- Database: Microsoft SQL Server.
- Backend: separate .NET project hosted on MonsterASP (REST API, business logic, DB access, Bosta integration, email transport, Hangfire scheduler, admin auth).
- Frontend: Next.js 16 (React 19) on Vercel. Pure React UI, no Next.js API routes.
- Data fetching: hybrid. Catalog (schools, grades, products) is server-rendered (SSR + ISR) for SEO; cart, checkout, order tracking, and admin are client-side. CORS locked to the Vercel origin; admin JWT in an httpOnly cookie.

---

# 13. Risks

| Risk | Impact | Mitigation |
|--------|---------|------------|
| Incorrect inventory updates | High | Inventory validation; diff-based notify-me trigger |
| Delivery delays | Medium | Shipment tracking (Bosta webhooks) |
| Email failures | Medium | Retry mechanism |
| Human errors | Medium | Validation rules |
| Seasonal demand spikes | High | Scalable architecture; atomic place-order decrement |
| Third-party API downtime | High | Retry & monitoring |
| Parent loses the per-order token URL | High | Token is the sole status access path; included in every order-update email |
| Stalled pre-handoff orders block stock and parent wait | Medium | Hangfire 5-day auto-cancel + restock scheduled job |
| Local image storage loss (no CDN) | Medium | Manual backups; storage monitoring |

---

# 14. Success Criteria

The project will be considered successful if:

- Customers successfully place online orders.
- Inventory remains accurate.
- Manual inventory work decreases.
- Orders are processed digitally.
- Delivery tracking functions correctly.
- Customer support requests decrease.
- Business reporting improves.

---

# 15. Future Business Roadmap

## Version 2

- Parent Accounts
- Online Payments
- POS Integration
- Coupons
- Loyalty Program
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

# 16. Out of Scope

The following features are intentionally excluded from Version 1:

- Parent login
- Online payments
- Refunds
- Loyalty program
- Coupons
- POS
- Supplier management
- Purchase orders
- Multiple stores
- Multiple cities
- SMS notifications
- WhatsApp notifications
- Mobile applications
- School contracts / bulk school orders
- Priced product bundles / set discounts
- Size guide / size charts
- Bilingual (name_ar / name_en) data columns
- CSV / Excel catalog import
- Batch Bosta pickup booking
- Customer self-service exchange (exchange is in-store, admin-logged only)
- Customer self-service cancellation token lookup (parent must use the email link)

---

# Appendix A - Business Process Summary

## Customer Process

```text
Browse Schools

↓

Browse Products

↓

Product Details

↓

Shopping Cart

↓

Checkout

↓

Order Processing

↓

Delivery

↓

Order Tracking
```

---

## Admin Process

```text
Login

↓

Dashboard

↓

Inventory Management

↓

Order Processing

↓

Exchange Management

↓

Reports
```

---

# Appendix B - Business Feature Prioritization (MoSCoW)

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

- Product Search
- Notify Me
- Full Uniform Set

---

## Could Have

- Advanced Analytics
- Delivery Dashboard
- Seasonal Reports

---

## Won't Have (Version 1)

- Parent Accounts
- Online Payments
- Refunds
- POS
- Coupons
- Loyalty Program
- SMS
- WhatsApp
- Mobile Applications
- Multi-Store
- Multi-City

---

# Appendix C - Version History

| Version | Date | Author | Description |
|----------|------|--------|-------------|
| 1.0 | July 2026 | Mohamed Zahran | Initial BRD |
| 1.1 | July 2026 | Mohamed Zahran | Aligned with product-owner interview Q1–Q31: added 5-day auto-cancel, order state machine, color-as-attribute, token-link order access, manual stock + diff trigger, add-full-set semantics, technical constraints (MS SQL + .NET + MonsterASP + Vercel + hybrid fetch), updated out-of-scope list |
| 1.2 | July 2026 | Mohamed Zahran | Locked remaining open items: password recovery = one-time recovery code at first admin login (BR-011); store-pickup counter flow = phone or order number lookup, admin "mark picked up", token URL backup; 10K products / 100K variants scale targets confirmed; email transport = Gmail SMTP; school search = MiniSearch frontend + MSSQL LIKE backend; admin auth = single JWT 8h sliding expiry no refresh token; audit log = state-changing actions only. Open Questions collapsed to ops-only items. |

---

# Appendix D - Order State Machine (Locked)

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
