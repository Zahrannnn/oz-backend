---
description: Build the project, start the API, and smoke test all endpoints.
agent: build
---

Verify the oz-backend API is working:

1. Kill any running dotnet process: `Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force`
2. Build: `dotnet build` from `src/Api` — report errors if any
3. Start: `dotnet run --project src/Api` with `ASPNETCORE_ENVIRONMENT=Development`
4. Wait 15 seconds for startup
5. Run smoke tests:

**Public endpoints:**
- `GET /api/v1/health` — expect 200 `{ status: "ok" }`
- `GET /api/v1/schools` — expect 200 with items array
- `GET /api/v1/products/1` — expect 200 or 404

**Admin auth:**
- `POST /api/v1/admin/auth/login` with `{ email: "admin@oz.com", password: "admin123" }` — expect 200 with token
- Use the token for all below

**Admin endpoints:**
- `GET /api/v1/admin/dashboard` — expect 200
- `GET /api/v1/admin/products?page=1&page_size=5` — expect 200 with items
- `GET /api/v1/admin/orders?page=1&page_size=5` — expect 200 with items
- `GET /api/v1/admin/audit-log?page=1&page_size=3` — expect 200 with items
- `GET /api/v1/admin/grade-stages?schoolId=1` — expect 200 with items
- `GET /api/v1/admin/item-types` — expect 200 with items

6. Report: pass/fail for each endpoint, any errors from stderr log
7. Kill the dotnet process when done
