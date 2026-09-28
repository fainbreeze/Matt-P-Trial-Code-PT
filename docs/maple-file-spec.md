# Maple Card Services - Transaction Export

Transaction file layout for Ledgerline integration.
Document version 0.9 (draft).

## Overview

Maple Card Services will send a daily file of card transactions for the merchants that we settle for.
The file is a CSV export from our settlement system.
One row is one transaction.

## File

| Item       | Value                |
|------------|----------------------|
| File name  | `maple_YYYYMMDD.csv` |
| Encoding   | UTF-8                |
| Separator  | Comma                |
| Header row | Required             |

## Columns

| Column        | Description                                                           |
|---------------|-----------------------------------------------------------------------|
| `ref`         | Maple's reference for the transaction. Numeric, for example `500101`. |
| `mid`         | Merchant ID.                                                          |
| `txn_date`    | Date of the transaction, `YYYY-MM-DD`.                                |
| `txn_time`    | Time of the transaction, `HH:mm:ss`.                                  |
| `txn_amt`     | Transaction amount.                                                   |
| `txn_code`    | Transaction code. `P` = purchase, `R` = refund, `V` = void.           |
| `terminal_id` | Terminal that took the payment.                                       |

## Notes

- All amounts are in the merchant's settlement currency.
- Maple may resend a transaction with the same `ref` if it was corrected.

## Sample

Copied from our test export:

```csv
ref,mid,txn_date,txn_time,txn_amt,txn_code,terminal_id
500101,M-3001,2026-09-01,09:12:45,23.50,P,T-0001
500102,M-3001,2026-09-01,09:40:02,64.00,P,T-0001
500103,M-3002,2026-09-01,10:15:30,12.99,P,T-0002
```

More sample files are in the `samples/maple/` folder.

## Contact

Questions about this document can go to the Maple integrations team through your Ledgerline account manager.
