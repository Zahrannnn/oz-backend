# Admin — Audit Log

All require auth. Route prefix: `/api/v1/admin/audit-log`

## GET /api/v1/admin/audit-log
Query: `?actor=<guid>&action=<string>&from=<datetime>&to=<datetime>&entity_type=<string>&page=1&page_size=20`
All filters optional. Sorted by `createdAt DESC`.
Response: `{ items: [{ id, actorId, action, entityType, entityId, beforeJson, afterJson, reason, createdAt }], total, page, page_size }`

## GET /api/v1/admin/audit-log/export
Same query params (no pagination). Returns CSV download.
Response: `Content-Type: text/csv`, `Content-Disposition: attachment; filename=audit-log-export.csv`
CSV columns: `Id,ActorId,Action,EntityType,EntityId,CreatedAt,Reason`
