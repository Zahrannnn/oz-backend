# Storefront — Products

## GET /api/v1/schools/{schoolId}/grade-stages/{gradeId}/products
Query: `?item_type=1&gender=1&page=1&page_size=20`
Response 200:
```json
{
  "items": [{
    "id": 1, "itemType": "T-Shirt", "gender": 1, "color": "احمر",
    "isInSet": false, "priceFrom": 120.00, "thumbnailUrl": null,
    "stockStatus": "in_stock",
    "variants": [{ "id": 1, "sizeLabel": "M", "priceInclVat": 120.00, "stock": 7 }]
  }], "total": 1, "page": 1, "page_size": 20, "has_next": false
}
```
Stock status: `in_stock` | `low_stock` | `out_of_stock`

## GET /api/v1/products/{id}
Response 200:
```json
{
  "id": 1, "schoolName": "Cairo Language School", "gradeStageName": "KG1",
  "itemType": "T-Shirt", "gender": 1, "color": null, "isInSet": false,
  "variants": [{ "id": 1, "sizeLabel": "M", "priceInclVat": 120.00, "stock": 7 }],
  "images": [], "createdAt": "...", "updatedAt": "..."
}
```
404 if product archived or not found.
