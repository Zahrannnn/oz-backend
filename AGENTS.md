# Oz Backend — Agent Instructions

## Project Overview
School uniform store backend. .NET 10 Web API, EF Core 10, MS SQL Server (LocalDB dev), Hangfire, FluentValidation, JWT auth, Bosta shipping integration.

## Build & Run
```bash
dotnet build                                    # build solution
dotnet run --project src/Api                    # start API on http://localhost:5000
dotnet ef migrations add <Name> --project src/Infrastructure   # create migration
dotnet ef database update --project src/Infrastructure          # apply migration
```

## Environment Variables
- **Canonical reference**: `.env.example` at repo root
- **Local dev**: copy `.env.example` to `.env`, fill in values
- **Production (MonsterASP)**: set in control panel — never commit
- **Required in non-Development**: `ConnectionStrings__Default`, `JWT_SECRET`, `BOSTA_API_KEY`, `BOSTA_WEBHOOK_SECRET` — startup fails fast if any missing
- **Convention**: `Section__Key` env var maps to `Section:Key` config (e.g. `ConnectionStrings__Default` → `ConnectionStrings:Default`)

## Project Structure
- `src/Api/Controllers/` — Controllers (Storefront/ and Admin/ subdirs)
- `src/Api/DTOs/` — Request/response DTOs with `[JsonPropertyName]`
- `src/Api/Services/` — JwtService, AuditLogService, SmtpEmailService, BostaClient
- `src/Api/Jobs/` — Hangfire jobs (SendEmailJob, AutoCancelOrdersJob)
- `src/Api/Helpers/` — Shared utilities (OrderHelpers)
- `src/Domain/Entities/` — EF Core entities
- `src/Infrastructure/Data/AppDbContext.cs` — Entity configs + seed data
- `src/Infrastructure/Repositories/` — Generic IRepository<T>
- `docs/` — PRD, BRD, SDD, DATABASE_DESIGN.md, API_DESIGN.md, BE_TASKS.md
- `docs/API_REFERENCE.md` — Slim index → `docs/api/*.md` section files
- `.opencode/agent/sprint-builder.md` — DeepSeek V4 Flash subagent for implementation

## Conventions
- Controllers: `[ApiController]`, `[Tags("...")]`, `[Route("api/v1/...")]`
- Admin controllers: `[Authorize]` on class
- Entity config: snake_case table names, `datetime2(3)`, `SYSUTCDATETIME()` defaults
- Audit log: `AuditLogService.WriteAsync(actorId, action, entityType, entityId, beforeJson, afterJson, reason)`
- Actor ID: `Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)`
- Pagination: `{ items, total, page, page_size }`
- Errors: RFC 7807 `application/problem+json`
- Soft delete: `POST /{id}/archive`, `DELETE` returns 405
- Order state strings: lowercase snake_case (placed, ready_to_ship, in_transit, etc)
- Email: single `SendEmailJob` class, HTML built inline at call site
- Order state/string conversion: `OrderHelpers.StateToString()` / `OrderHelpers.ChannelToString()`
- No comments in code unless explicitly requested
- No documentation files unless explicitly requested

## Sprint Workflow
1. Read `BE_TASKS.md` for next sprint section
2. Group tasks by dependencies (parallel where possible)
3. Delegate each group to `sprint-builder` agent via Task tool
4. Each agent: read existing files → implement → `dotnet build` → return files changed
5. Commit each agent's work as separate commit
6. Verify: build + start app + smoke test endpoints
7. Update `docs/api/*.md` with new endpoint docs
8. Ponytail review for over-engineering

## Auth
- Default admin: `admin@oz.com` / `admin123` (seeded by AdminInitializer)
- JWT in `admin_session` cookie (HttpOnly, Secure, SameSite=Lax, 8h) + Bearer header
- Login: `POST /api/v1/admin/auth/login` → `{ adminId, email, token, expiresAt }`

## Key Files Reference
- `src/Api/Program.cs` — DI registration, middleware pipeline, Hangfire recurring jobs
- `src/Infrastructure/Data/AppDbContext.cs` — All entity configs, seed data, migration source
- `docs/api/` — Split API reference files (one per domain area)
- `BE_TASKS.md` — Task list with acceptance criteria, sprints, dependencies
- `docs/DATABASE_DESIGN.md` — Authoritative DB schema spec
- `docs/API_DESIGN.md` — Authoritative API spec
