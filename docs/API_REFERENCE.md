# API Reference — Oz Backend

Base URL: `http://localhost:5000` (dev)

## Conventions

- **Auth**: Public endpoints = no auth. Admin = `Authorization: Bearer <token>` header OR `admin_session` cookie (HttpOnly). Login at `POST /api/v1/admin/auth/login`.
- **Pagination**: `{ items: [], total, page, page_size }`. Query: `?page=1&page_size=20` (max 100).
- **Errors**: RFC 7807 — `{ type, title, status, detail, errors? }`.
- **Timestamps**: UTC ISO 8601.
- **Soft delete**: `POST /{id}/archive`. `DELETE` returns 405.

## Endpoint Map

| File | Section | Endpoints |
|------|---------|-----------|
| [health.md](api/health.md) | Health | `GET /health`, `GET /readyz` |
| [storefront-schools.md](api/storefront-schools.md) | Storefront — Schools | `GET /schools`, `GET /schools/{id}`, `GET /schools/{id}/grade-stages` |
| [storefront-products.md](api/storefront-products.md) | Storefront — Products | `GET /schools/{sid}/grade-stages/{gid}/products`, `GET /products/{id}` |
| [storefront-orders.md](api/storefront-orders.md) | Storefront — Orders | `POST /orders`, `GET /orders/by-token/{token}` |
| [admin-auth.md](api/admin-auth.md) | Admin — Auth | login, logout, me, forgot-password, verify-recovery-code |
| [admin-schools.md](api/admin-schools.md) | Admin — Schools | CRUD + archive |
| [admin-products.md](api/admin-products.md) | Admin — Products | product + variant CRUD, images, stock edit |
| [admin-catalog.md](api/admin-catalog.md) | Admin — Catalog | grade-stage + item-type CRUD |
| [admin-dashboard.md](api/admin-dashboard.md) | Admin — Dashboard | `GET /dashboard` |
| [admin-orders.md](api/admin-orders.md) | Admin — Orders | list, detail, transition, bosta-pickup, mark-picked-up, run-auto-cancel |
| [admin-audit.md](api/admin-audit.md) | Admin — Audit Log | list + CSV export |
| [webhooks.md](api/webhooks.md) | Webhooks | Bosta tracking webhook |
| [entity-reference.md](api/entity-reference.md) | Entity Reference | enums, field types |
