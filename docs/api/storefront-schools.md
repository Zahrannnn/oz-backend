# Storefront — Schools

## GET /api/v1/schools
Query: `?q=search_text&page=1&page_size=20`
Response 200:
```json
{ "items": [{ "id": 1, "name": "Cairo Language School", "type": "Language", "typeLabel": "لغات" }], "total": 3, "page": 1, "page_size": 20 }
```

## GET /api/v1/schools/{id}
Response 200: same item shape. 404 if missing.

## GET /api/v1/schools/{schoolId}/grade-stages
Response 200:
```json
{ "items": [{ "id": 1, "schoolId": 1, "name": "KG1", "displayOrder": 1 }] }
```
