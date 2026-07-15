# Entity Field Reference

| Field | Type | Notes |
|-------|------|-------|
| Gender | byte | 1=Boys, 2=Girls, 3=Unisex |
| SchoolType | byte | 1=Governmental (حكومي), 2=Experimental (تجريبي), 3=Arabic (عربي), 4=Language (لغات), 5=International (دولي), 6=Private (خاص) |
| OrderState | byte | 1=placed, 2=ready_to_ship, 3=handed_to_courier, 4=in_transit, 5=delivered, 6=cod_failed, 7=returned_to_store, 8=ready_for_pickup, 9=picked_up, 10=closed_success, 11=closed_failed, 12=cancelled |
| OrderChannel | byte | 1=delivery, 2=pickup |
| EmailStatus | byte | 0=pending, 1=success, 2=failed |
| color | string | Arabic text (e.g. "احمر") |
| priceInclVat | decimal | Price including VAT |
| stock | int | Current inventory |
| deliveryFee | decimal | Currently 0 (free) |
| trackingUrl | string | `{base}/orders/{token}` |
| trackingTokenHash | binary(32) | SHA-256 of URL token |
