# Admin — Auth

Route prefix: `/api/v1/admin/auth`

## POST /api/v1/admin/auth/login
Body: `{ "email": "admin@oz.com", "password": "admin123" }`
Response 200: `{ adminId, email, token, expiresAt }` + sets `admin_session` cookie (HttpOnly, Secure, SameSite=Lax, 8h).
Errors: 401 `{ error: "invalid_credentials" }` | 423 `{ error: "account_locked", lockedUntil }`

## POST /api/v1/admin/auth/logout
Response 200: `{ message: "logged_out" }`

## GET /api/v1/admin/auth/me
Requires auth. Response 200: `{ adminId, email, expiresAt }` | 401

## POST /api/v1/admin/auth/forgot-password
Body: `{ "email": "admin@oz.com" }`
Always returns 200 (no enumeration). If email exists:
```json
{ "message": "If the email exists, a recovery code has been generated.", "code": "258120" }
```
If not found: `{ "message": "..." }` (no code field)

## POST /api/v1/admin/auth/verify-recovery-code
Body: `{ "email": "admin@oz.com", "code": "258120" }`
Responses:
- 200: `{ adminId, email, token, expiresAt }` + sets cookie (same as login)
- 401: `{ error: "invalid_code" }` (increments attempts, locks after 5)
- 410: `{ error: "code_expired" }`
