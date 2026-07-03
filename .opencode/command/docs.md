---
description: Scan all controllers and update docs/api/ section files with current endpoints.
agent: build
---

Update the API reference documentation by scanning the actual controller code:

1. Read all controller files in `src/Api/Controllers/` (including subdirs `Admin/`, `Storefront/`, `Webhooks/`)
2. For each controller, extract:
   - Route prefix (from `[Route]` attribute)
   - HTTP methods + paths (from `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` attributes)
   - Auth requirement (presence of `[Authorize]`)
   - Request body type (from `[FromBody]` parameter)
   - Query parameters (from `[FromQuery]` parameters)
   - Response shape (from the return type or `Ok(...)` / `Created(...)` calls)

3. Read existing docs in `docs/api/` to understand the current format

4. Update each section file in `docs/api/` with accurate endpoint documentation:
   - Route, method, auth requirement
   - Query params with types and defaults
   - Request body JSON example
   - Response JSON example
   - Error codes

5. Update `docs/API_REFERENCE.md` index if any new section files are needed

6. Commit with message: `docs: update API reference from controller scan`
