# Health

## GET /api/v1/health
No auth. Response 200: `{ status: "ok" }`

## GET /api/v1/readyz
No auth. Response 200: `{ status, timestamp }` | 503: `{ status: "unhealthy", failures: [] }`
