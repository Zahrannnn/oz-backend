# Admin — Schools

All require auth. Route prefix: `/api/v1/admin/schools`

## GET /api/v1/admin/schools
Query: `?page=1&page_size=20`
Response: `{ items: [{ id, name, type, isArchived, createdAt }], total, page, pageSize }`

## GET /api/v1/admin/schools/{id}
Response: `{ id, name, type, isArchived, createdAt }` | 404

## POST /api/v1/admin/schools
Body: `{ "name": "New School", "type": 1 }`
type: 1=National, 2=Experimental, 3=Arabic, 4=Language, 5=International, 6=Private

## PUT /api/v1/admin/schools/{id}
Body: same as POST

## POST /api/v1/admin/schools/{id}/archive
Soft delete. Response: `{ message: "School archived" }`

## DELETE /api/v1/admin/schools/{id}
Returns 405. Use archive instead.
