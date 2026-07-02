# Admin — Dashboard

## GET /api/v1/admin/dashboard
Requires auth. Response 200:
```json
{
  "revenueThisMonth": 0.0,
  "ordersToday": 2,
  "pendingOrders": 2,
  "lowStockCount": 0,
  "recentActivity": [
    { "id": 17, "action": "admin.login_success", "entityType": "admin", "entityId": "guid", "createdAt": "..." }
  ],
  "lowStockVariants": [
    { "id": 1, "sizeLabel": "M", "stock": 2, "lowStockThreshold": 5, "productName": "T-Shirt" }
  ]
}
```
- `revenueThisMonth`: sum of `total` from orders with state `closed_success` this month
- `ordersToday`: count of orders created today
- `pendingOrders`: count where state in `placed`, `ready_to_ship`, `ready_for_pickup`
- `lowStockCount`: variants where `stock <= lowStockThreshold` and not archived
- `recentActivity`: last 10 audit log entries
- `lowStockVariants`: first 10 low-stock variants with product name
