# Sprint Plan

# School Uniform Store Platform

**Version:** 1.0
**Author:** Mohamed Zahran (Team Lead)
**Status:** Draft
**Sprint Cadence:** 2 weeks
**Target:** 8 sprints (16 weeks)
**Teams:** Design, Frontend, Backend, QA

---

## Team Roles

| Team | Headcount | Lead |
|------|-----------|------|
| **Design** | 1 UI/UX | Creates wireframes, mockups, prototypes for storefront + admin |
| **Frontend** | 2 devs | Next.js 16 (React 19), RTL Arabic storefront + admin panel |
| **Backend** | 2 devs | .NET 10 REST API, MS SQL Server, Hangfire, Bosta, SMTP |
| **QA** | 1 engineer | Test plans, manual + automated, regression, smoke |

---

## Dependencies

```
DB schema must exist before BE APIs.
BE APIs must exist before FE integration.
Design mockups must exist before FE implementation.
```

---

## Sprint 0 — Foundation & Setup (Weeks 1–2)

### Design
- Storefront wireframes: landing → schools → grades → product grid → product detail → cart → checkout → confirmation → tracking
- Admin wireframes: login → dashboard → orders → products → schools → inventory → reports → exchanges → audit log
- RTL layout spec, typography scale, color palette
- Shared component list (cards, buttons, inputs, modals, tables, forms)

### Frontend
- `npx create-next-app@16` project scaffold with App Router
- RTL setup (`next-intl` or `next-i18next` with `ar` as default locale / pure RTL via CSS)
- Tailwind + shadcn/ui RTL-aware component library setup
- Vercel deployment pipeline (preview deploys per branch)
- Skeleton routing: `/` → `/schools/[id]/grades/[id]` → `/products/[id]` → `/cart` → `/checkout` → `/orders/token/[token]` → `/admin/*`
- Shared layout: header, footer, Navbar, loading states

### Backend
- .NET 10 solution scaffold (Web API project)
- EF Core + SQL Server connection string (dev: localdb; prod: MonsterASP-hosted)
- Repository pattern + DTO layer + FluentValidation setup
- Hangfire server + dashboard setup
- Health check endpoint (`GET /v1/health`)
- `docker-compose.yml` for local dev (SQL Server container + .NET API)
- Postman collection skeleton

### DB
- First migration: `school`, `grade_stage`, `item_type` tables
- Seed data: 3 sample schools + grade-stages + item-types

### QA
- Test plan document
- Test case template
- Bug tracker setup (GitHub Issues / Linear)

**Deliverables:** Dev environments running, wireframes approved, project scaffolds deployable, test plan reviewed.

---

## Sprint 1 — Catalog Core (Weeks 3–4)

### Design
- Admin product management mockups (product CRUD, variant editor, image upload)
- Mobile responsive variants for product grid + detail

### Frontend

| Feature | Notes |
|---------|-------|
| Landing page — school list | SSR + ISR; MiniSearch client-side search |
| School > grade selector | Chips: Ebtda2y / E3dady / Sanawy |
| Product grid (by school+grade) | Stock badge, price, image |
| Product detail page | Gallery, size selection, stock status, add to cart |
| Add full set button | Expands to (school+grade+gender) set items |
| Admin: product list + create/edit | |
| Admin: school management | |

### Backend

| API | Notes |
|-----|-------|
| `GET /v1/schools` + `?q=` | ISR-friendly, LIKE fallback |
| `GET /v1/schools/{id}/grades` | |
| `GET /v1/schools/{schoolId}/grades/{id}/products` | Products + variants + stock + price |
| `GET /v1/products/{id}` | Full detail + variants + images |
| `POST/PUT/DELETE /v1/admin/schools` | Admin CRUD |
| `POST/PUT/DELETE /v1/admin/products` | Admin CRUD |
| `POST/PUT/DELETE /v1/admin/products/{id}/variants` | Variant CRUD |
| `POST /v1/admin/products/{id}/images` + delete | Image upload to `/uploads` |

### DB Migration
- `product`, `product_grade`, `variant`, `product_image` tables
- Foreign keys, indexes on `school.is_active`, `product.school_id`, `variant.product_id`

### QA
- Catalog browsing test cases
- Admin CRUD test cases (products, schools)
- Image upload test cases

**Deliverables:** Storefront shows schools + products with stock. Admin can manage schools/products/variants/images.

---

## Sprint 2 — Cart + Checkout (Weeks 5–6)

### Design
- Checkout form wireframe
- Order confirmation page design
- Error states (OOS at checkout, invalid form)

### Frontend

| Feature | Notes |
|---------|-------|
| Shopping cart | Client-side (localStorage); quantity + remove + total |
| Checkout form | Name, phone, email, address, delivery/pickup selection |
| Order confirmation page | Token URL displayed, "Save this link" prompt |
| Cart persistence | localStorage; survives page refresh |

### Backend

| API | Notes |
|-----|-------|
| `POST /v1/orders` | Atomic tx: UPDLOCK on variant → check stock → deduct → INSERT order → INSERT items → generate token → COMMIT. 201 + token. 409 if OOS. |
| SMTP service | Gmail SMTP integration; SendOrderConfirmationEmail |

### DB Migration
- `orders`, `order_item` tables
- Token hash column (SHA2-256), unique index
- `channel` (delivery/pickup), `parent_email`, `cod_amount`

### QA
- Place order happy path (EO2E via API)
- OOS rollback test (concurrent orders for same variant)
- SMTP email delivery test

**Deliverables:** End-to-end checkout flow works. Order created atomically. Confirmation email sent.

---

## Sprint 3 — Admin Auth + Management (Weeks 7–8)

### Design
- Admin dashboard KPI widget mockups
- Audit log viewer design

### Frontend

| Feature | Notes |
|---------|-------|
| Admin login page + redirect | JWT cookie; 8h sliding |
| Admin dashboard | Revenue, orders, low-stock, pending orders widgets |
| Admin: grade-stage management | |
| Admin: item-type management | |
| Admin: stock edit UI | Per variant; saves new value; diff computed server-side |
| Admin: audit log viewer | Filterable (actor, action, date, target); export CSV |

### Backend

| API | Notes |
|-----|-------|
| `POST /v1/auth/login` | bcrypt verify; failed-attempts counter; 423 lockout at 5 failures; JWT cookie |
| `GET /v1/auth/me` | Validate JWT, return admin profile |
| `POST /v1/auth/forgot-password` | One-time recovery code flow |
| `CRUD /v1/admin/grade-stages` | |
| `CRUD /v1/admin/item-types` | |
| `PUT /v1/admin/variants/{id}/stock` | Diff-based trigger (notify-me, low-stock) |
| `GET /v1/admin/audit-log` | Filterable + paginated |
| `GET /v1/admin/audit-log/export` | CSV download |
| `GET /v1/admin/dashboard` | KPI aggregation queries |

### DB Migration
- `admin`, `password_recovery`, `audit_log` tables
- `pending_alert` table (written here, used Sprint 6)

### QA
- Login failure + lockout test
- One-time recovery code test
- Stock edit + diff trigger test
- Audit log write + read test

**Deliverables:** Admin can log in, manage all catalog entities, edit stock, view dashboard and audit log.

---

## Sprint 4 — Orders + Bosta Integration (Weeks 9–10)

### Design
- Admin order processing UI mockups (state transitions, Bosta button)

### Backend (BE-heavy sprint)

| API / Job | Notes |
|-----------|-------|
| `GET /v1/admin/orders` | Filterable (status, date, school, courier) |
| `GET /v1/admin/orders/{id}` | Full detail + items + timeline |
| `POST /v1/admin/orders/{id}/transition` | State machine guard (whitelist per state) |
| `POST /v1/admin/orders/{id}/bosta-pickup` | Bosta create-shipment API; HMAC response verification |
| `POST /v1/webhooks/bosta` | HMAC-verified; handles delivered/cod_failed/returned |
| Hangfire: `AutoCancelOrphansJob` | Daily 03:00; cancels + restocks pre-handoff orders > 5d |
| Hangfire: `SendOrderUpdateEmail` | Shipped, delivered, COD-failed, cancelled emails |

### Frontend

| Feature | Notes |
|---------|-------|
| Admin order list + detail | Filter, search, view timeline |
| Admin order status transition buttons | Guarded by state machine; only valid next states shown |
| Admin Bosta pickup booking button | Appears when state = ready_to_ship |

### DB Migration
- `bosta_tracking_id` on orders, unique index
- `handed_to_courier_at`, `in_transit_at`, `delivered_at`, `returned_at` timestamps

### QA
- Full order lifecycle E2E (create → process → bosta pickup → webhook delivered)
- Auto-cancel job (mock 5-day cutoff)
- Bosta webhook HMAC verification test
- Invalid state transition rejection test

**Deliverables:** Full order lifecycle automated. Bosta integration live. Auto-cancel running.

---

## Sprint 5 — Pickup + Exchanges + Reports (Weeks 11–12)

### Design
- Admin pickup counter UI
- Exchange form mockups
- Reports dashboard mockups (charts, filters)

### Backend

| API | Notes |
|-----|-------|
| `GET /v1/admin/orders/pickup-list` | Orders in ready_for_pickup |
| `GET /v1/admin/orders?phone=` | Phone number lookup for counter |
| `POST /v1/admin/orders/{id}/mark-picked-up` | State → picked_up → closed_success |
| `POST /v1/admin/orders/{id}/exchanges` | Multi-step tx: refund stock → take stock → price delta → update total |
| `GET /v1/admin/reports/sales` | Day/week/month/school/channel aggregates |
| `GET /v1/admin/reports/inventory` | Stock per variant + low-stock list |
| `GET /v1/admin/reports/notify-me` | Demand ranking (most-requested OOS variants) |

### Frontend

| Feature | Notes |
|---------|-------|
| Admin pickup counter UI | Phone/order search → order detail → mark picked up |
| Admin exchange form | Select item → select new size → reason → submit |
| Admin reports dashboard | Sales, inventory, orders, notify-me charts |

### QA
- Exchange flow test (stock refund + take + price delta + total update)
- Pickup counter lookup test
- Reports aggregation accuracy test (compare against raw DB queries)

**Deliverables:** Pickup counter flow operational. Exchange management complete. Reports dashboard shows real data.

---

## Sprint 6 — Notify-Me + Token Tracking + Polish (Weeks 13–14)

### Backend

| API / Job | Notes |
|-----------|-------|
| `POST /v1/variants/{id}/notify-me` | Store pending_alert; dedup by (variant_id, email_hash) |
| Restock diff trigger (in stock edit) | Query pending_alerts where variant_id = X AND notified = false; send email; update notified=true |
| Hangfire: `SendNotifyMeEmail` | Async email per pending alert |

### Frontend

| Feature | Notes |
|---------|-------|
| Notify-me UI on OOS product detail | Email input + "Notify Me" button |
| Token order tracking page | Status timeline (ordered→shipped→delivered) |
| Cancel button on order tracking (if pre-handoff) | |
| Admin audit log CSV export | |
| Admin low-stock widget on dashboard | |

### Polish (Both Teams)

| Area | Items |
|------|-------|
| FE | Loading skeletons, empty states, error toasts, mobile responsive RTL review, keyboard nav, screen reader labels |
| BE | Input validation hardening, consistent error response format, rate limiting (5 req/s per IP on checkout, 30 req/min on admin) |
| Harden | Anti-bot token URL enumeration (no timing side-channel), JWT cookie SameSite=Strict+Secure+HttpOnly, CORS lock to Vercel origin, SQL injection (EF Core safe), XSS (React safe), CSRF (SameSite=Strict prevents) |

### QA
- Notify-me full flow (OOS → subscribe → admin restock → email received)
- Token tracking page test (valid + expired/invalid token)
- Full regression pass (all prior sprints)
- Security scan (headers, rate limiting, auth bypass attempts)

**Deliverables:** Notify-me flow operational. Token tracking page live. All security hardening applied. Full regression green.

---

## Sprint 7 — Deployment & Launch Prep (Weeks 15–16)

### Backend (Ops)

| Task | Notes |
|------|-------|
| Bosta production API credentials + configure | Sandbox → production cutover |
| Gmail SMTP SPF/DKIM/DMARC setup | For sender domain |
| MonsterASP deployment | Connection string → production SQL Server tier; enable HTTPS; configure `/uploads` |
| Hangfire dashboard locked to admin IP | |
| CORS lock to Vercel production origin | |

### Frontend (Ops)

| Task | Notes |
|------|-------|
| Vercel production deployment | Connect repo, set env vars |
| Custom domain setup | |
| ISR revalidation tuning | |
| Analytics (Vercel Analytics / GA4) | |

### QA

| Task | Notes |
|------|-------|
| Production smoke test | Critical paths: browse → checkout → order → email → admin login → process → Bosta |
| Performance check | Lighthouse, Core Web Vitals, API response times |
| Data seeding | Import real schools, grade-stages, item-types, products, variants, images |

### Docs

| Task | Notes |
|------|-------|
| API docs final review | |
| Deployment runbook | |
| Admin user guide | |
| Backup + restore procedure | |

**Deliverables:** Production live. All ops credentials configured. Smoke test green. Runbook handed off.

---

## Summary Timeline

```
Sprint    Weeks    Theme                         BE   FE   D   QA
─── ─────── ─────────────────────────────── ─── ─── ─── ───
 0   1–2     Foundation & Setup              X    X    X   X
 1   3–4     Catalog Core                    X    X    X   X
 2   5–6     Cart + Checkout                 X    X    X   X
 3   7–8     Admin Auth + Management         X    X    X   X
 4   9–10    Orders + Bosta Integration      X    X    ─   X
 5   11–12   Pickup + Exchanges + Reports    X    X    X   X
 6   13–14   Notify-Me + Polish + Harden     X    X    ─   X
 7   15–16   Deployment & Launch Prep        X    X    ─   X
```

## Key Risks

| Risk | Mitigation |
|------|------------|
| Bosta sandbox vs production differences | Start sandbox integration Sprint 4; leave Sprint 7 for prod credential cutover |
| Gmail SMTP daily cap (500/day free, 2000/day Workspace) | Monitor email volume in Sprint 6; budget Workspace upgrade if needed |
| MonsterASP SQL Server tier capacity unknown | Load-test with 10K products + 100K variants in Sprint 6; scale tier if needed |
| Single BE dev knowledge bottleneck | Document all Bosta/Hangfire/Auth patterns in code; peer review every PR |
| RTL UI issues late in project | Design validates RTL from Sprint 0; FE uses RTL-aware components from Sprint 0 |
