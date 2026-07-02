# Admin — Products, Variants, Images, Stock

All require auth.

## Products — Route: `/api/v1/admin/products`

### GET /api/v1/admin/products
Query: `?schoolId=1&gradeStageId=1&itemTypeId=1&gender=1&page=1&page_size=20`
Response: `{ items: [{ id, schoolName, gradeStageName, itemType, gender, color, isInSet, isArchived, createdAt, updatedAt }], total, page, page_size }`

### POST /api/v1/admin/products
Body: `{ schoolId, gradeStageId, itemTypeId, gender, color? }` (gender: 1=Boys, 2=Girls, 3=Unisex)

### PUT /api/v1/admin/products/{id}
Body: same as POST

### POST /api/v1/admin/products/{id}/archive
Soft delete.

### PUT /api/v1/admin/products/{id}/set-flag
Toggles `isInSet`.

## Variants — Route: `/api/v1/admin/products/{productId}/variants` + `/api/v1/admin/variants/{id}`

### POST /api/v1/admin/products/{productId}/variants
Body: `{ sizeLabel, priceInclVat, stock?, reserved?, lowStockThreshold? }`
Response 201: `{ id, productId, sizeLabel, priceInclVat, stock, reserved, lowStockThreshold, isArchived, createdAt, updatedAt }`

### PUT /api/v1/admin/variants/{id}
Body: `{ sizeLabel, priceInclVat, stock, reserved, lowStockThreshold }`

### POST /api/v1/admin/variants/{id}/archive
Soft delete.

### PUT /api/v1/admin/variants/{id}/stock
Body: `{ stock, reason?, threshold? }`
UPDLOCK transaction → update stock + threshold → audit log.
Response 200: full variant object | 404

## Images — Route: `/api/v1/admin/products/{productId}/images`

### POST /api/v1/admin/products/{productId}/images
Multipart form-data, field `file`. Max 5 MB. Allowed: jpg, jpeg, png, webp.
Response 201: `{ id, productId, url, sortOrder }`

### DELETE /api/v1/admin/products/{productId}/images/{imageId}
Response 204: NoContent
