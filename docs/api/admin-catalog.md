# Admin — Grade Stages + Item Types

All require auth.

## Grade Stages — Route: `/api/v1/admin/grade-stages`

### GET /api/v1/admin/grade-stages
Query: `?schoolId=1&page=1&page_size=20`
Response: `{ items: [{ id, schoolId, name, displayOrder, createdAt }], total, page, page_size }`

### GET /api/v1/admin/grade-stages/{id}
Response: `{ id, schoolId, name, displayOrder, createdAt }` | 404

### POST /api/v1/admin/grade-stages
Body: `{ schoolId, name, displayOrder }`

### PUT /api/v1/admin/grade-stages/{id}
Body: same as POST

### POST /api/v1/admin/grade-stages/{id}/archive
Deletes entity (no soft-delete flag on GradeStage).

### DELETE /api/v1/admin/grade-stages/{id}
Returns 405.

## Item Types — Route: `/api/v1/admin/item-types`

### GET /api/v1/admin/item-types
Query: `?page=1&page_size=20`
Response: `{ items: [{ id, name, createdAt }], total, page, pageSize }`

### GET /api/v1/admin/item-types/{id}
Response: `{ id, name, createdAt }` | 404

### POST /api/v1/admin/item-types
Body: `{ name }`

### PUT /api/v1/admin/item-types/{id}
Body: `{ name }`

### POST /api/v1/admin/item-types/{id}/archive
Deletes entity.

### DELETE /api/v1/admin/item-types/{id}
Returns 405.
