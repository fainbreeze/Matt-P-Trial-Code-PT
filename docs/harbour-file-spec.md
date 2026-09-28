# Harbour Bank - Daily Transaction File Specification

Version 1.2

## 1. Overview

Harbour Bank sends one file per day to Ledgerline.
The file lists the card transactions that Harbour processed for merchants on that day.
Each file is a batch of independent rows.
One row is one transaction.

## 2. File format

| Item          | Value                                                        |
|---------------|--------------------------------------------------------------|
| File name     | `harbour_YYYYMMDD.csv`                                       |
| Encoding      | UTF-8, no byte order mark                                    |
| Separator     | Comma (`,`)                                                  |
| Header row    | Required, must be the first line                             |
| Line ending   | LF or CRLF                                                   |
| Quoting       | Double quotes, only when a value contains a comma or a quote |
| Rows per file | One transaction per row                                      |

`YYYYMMDD` in the file name is the business date of the file.

## 3. Columns

The columns must appear in this order, with these exact names.

| # | Column                 | Required | Format                    | Description                                                                                  |
|---|------------------------|----------|---------------------------|----------------------------------------------------------------------------------------------|
| 1 | `transaction_id`       | Yes      | Text, up to 50 characters | Harbour's unique ID for the transaction.                                                     |
| 2 | `merchant_id`          | Yes      | Text, up to 20 characters | The merchant that took the payment.                                                          |
| 3 | `transaction_datetime` | Yes      | ISO 8601 with UTC offset  | Date and time of the transaction.                                                            |
| 4 | `amount`               | Yes      | Decimal                   | Positive amount, exactly 2 decimal places, `.` as decimal separator, no thousands separator. |
| 5 | `currency`             | Yes      | `CAD` or `USD`            | Currency of the amount.                                                                      |
| 6 | `transaction_type`     | Yes      | `SALE` or `REFUND`        | Kind of transaction.                                                                         |
| 7 | `auth_code`            | Yes      | 6 alphanumeric characters | Authorisation code from the card network.                                                    |

## 4. Value formats

### 4.1 transaction_id

Transaction IDs use the `HB-` prefix.
Transactions issued before 2026 may have numeric IDs.

### 4.2 transaction_datetime

Format: `YYYY-MM-DDTHH:mm:ss` followed by a UTC offset.
The offset is either `Z` (UTC) or `+HH:mm` / `-HH:mm`.
Examples: `2026-09-01T09:15:00-07:00`, `2026-09-01T16:05:44Z`.

### 4.3 amount

The amount is always positive.
The direction of the money is given by `transaction_type`.
Refunds are sent as a positive amount with `transaction_type` set to `REFUND`.
Examples: `45.20`, `1200.00`.

### 4.4 currency and transaction_type

Values are upper case.
No other values are sent.

### 4.5 auth_code

Six characters, letters and digits only, upper case.

## 5. Example

```csv
transaction_id,merchant_id,transaction_datetime,amount,currency,transaction_type,auth_code
HB-100101,M-2001,2026-09-01T09:15:00-07:00,45.20,CAD,SALE,A1B2C3
HB-100103,M-2002,2026-09-01T12:45:10-04:00,19.99,USD,SALE,G7H8J9
HB-100104,M-2002,2026-09-01T13:30:00-04:00,19.99,USD,REFUND,K1L2M3
HB-100105,M-2003,2026-09-01T16:05:44Z,250.50,CAD,SALE,N4P5Q6
```

## 6. Delivery

Harbour delivers the file once per business day.
Files are not sent again after delivery.
