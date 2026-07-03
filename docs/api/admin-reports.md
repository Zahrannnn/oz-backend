# Admin — Reports

All require auth. Route prefix: `/api/v1/admin/reports`

## GET /api/v1/admin/reports/sales
Query: `?from=<datetime>&to=<datetime>&group_by=day|week|month&school_id=<long>&channel=<delivery|pickup>`

Aggregates revenue and order count for orders in state `closed_success` only.
- `from`, `to`: required (UTC ISO 8601)
- `group_by`: default `day`
  - `day`: `yyyy-MM-dd`
  - `week`: `yyyy-'W'ww`
  - `month`: `yyyy-MM`
- `school_id` (optional): filter by orders whose items belong to a product of that school
- `channel` (optional): `delivery` or `pickup`

Response 200:
```json
{
  "rows": [
    { "period": "2026-07-01", "orders_count": 5, "revenue": 600.00 }
  ],
  "totals": { "orders_count": 5, "revenue": 600.00 }
}
```

## GET /api/v1/admin/reports/inventory
Lists all non-archived variants with current stock vs threshold.

Response 200:
```json
{
  "variants": [
    { "id": 1, "product_name": "T-Shirt", "size": "M", "stock": 50, "threshold": 5, "status": "ok" }
  ],
  "low_stock_count": 0
}
```
- `status`: `out_of_stock` (stock == 0) | `low_stock` (stock <= threshold) | `ok`
- Ordered by stock ascending (lowest first)

## GET /api/v1/admin/reports/notify-me
Lists variants with active (un-notified) notify-me requests, ordered by request count DESC.

Response 200:
```json
{
  "variants": [
    { "id": 1, "product_name": "Polo", "size": "L", "request_count": 3 }
  ]
}
```
