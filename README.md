# TxnIngest

TxnIngest is a command line tool for Ledgerline, a payment processor.
Partners send us daily files of card transactions that they processed for merchants.
TxnIngest reads these files, validates each row, and stores the good rows.
It then produces a response file that tells the partner which rows we accepted or rejected, and why.

Today TxnIngest supports one partner: Harbour Bank.
The tool is small, and the whole code base is meant to be readable in one sitting.

## Prerequisites

- .NET 8 SDK

No other services, network access, or secrets are needed.

## Build, test, run

```bash
dotnet build
dotnet test
```

Ingest a file:

```bash
dotnet run --project src/TxnIngest.Cli -- ingest --source harbour --file samples/harbour/harbour_20260901.csv
```

List received files, newest first:

```bash
dotnet run --project src/TxnIngest.Cli -- receipts list
```

Show the response file for one receipt:

```bash
dotnet run --project src/TxnIngest.Cli -- receipts show <token>
```

Delete and recreate the database, and load a few Harbour transactions:

```bash
dotnet run --project src/TxnIngest.Cli -- db reset --seed
```

Use `db reset` without `--seed` for an empty database.

## Exit codes

| Code | Meaning                                                            |
|------|--------------------------------------------------------------------|
| 0    | The file was processed. Some rows may have been rejected.          |
| 1    | The file failed as a whole, for example because of a wrong header. |
| 2    | Bad arguments.                                                     |

## Database

Data is stored in a SQLite file named `txningest.db` in the current working directory.
Set the environment variable `TXNINGEST_DB` to use a different path.
The schema is created on first run with `EnsureCreated`.
There are no migrations, so after a schema change run `db reset`.

## Sample files

| File                                     | Content                              |
|------------------------------------------|--------------------------------------|
| `samples/harbour/harbour_20260901.csv`   | Harbour file, all rows valid         |
| `samples/harbour/harbour_20260902.csv`   | Harbour file, valid and invalid rows |
| `samples/harbour/harbour_bad_header.csv` | Harbour file with a wrong header     |
| `samples/maple/maple_20260901.csv`       | Maple sample file                    |
| `samples/maple/maple_20260902.csv`       | Maple sample file                    |

Partner specs are in `docs/`.

## How it works

`IngestionService.IngestAsync` handles one file in these steps:

1. **File duplicate check.**
   It computes the SHA-256 hash of the content.
   If a receipt with the same hash exists, it records a new receipt with state `DuplicateFile` and stops.
2. **Create receipt.**
   It records a receipt in state `Received` with a new token, the source, the file name, the hash, and the received time.
3. **Read rows.**
   It reads the CSV with CsvHelper into row objects, where every value is a string.
   If the header does not match or the CSV is malformed, the receipt is set to `Failed` with a reason, and processing stops.
4. **Adapt.**
   An adapter turns each row into `TransactionValues`.
   It parses text into typed values and does no validation.
   Text that cannot be parsed becomes `null`.
   Source columns that we keep but do not model go to `UnmappedValues`.
5. **Validate.**
   `Transaction.Create` checks every domain rule and returns all errors for a row.
6. **Duplicate row check.**
   A row is a duplicate if its external ID is already stored, or appeared earlier in the same file.
7. **Save.**
   In one database transaction, it saves the accepted transactions and one `RowResult` per row.
   It sets the receipt to `Completed` with the row counts.

`receipts show` builds the response file from the receipt and its row results.
Errors name the partner's column, not our internal field name.

### Domain rules

A transaction needs an external ID (up to 50 characters), a merchant ID (up to 20 characters), a UTC time that is not in the future, and an amount greater than 0 with at most 2 decimal places.
It also needs a currency (`CAD` or `USD`) and a type (`Purchase` or `Refund`).
The amount is always positive.
The direction of the money is in the type.

## Project layout

```
TxnIngest.sln
README.md
TASK.md
docs/                       Partner file specs
samples/                    Sample files for each partner
src/
  TxnIngest.Cli/            Entry point and commands (ingest, receipts, db)
  TxnIngest.Core/
    Domain/                 Transaction, values, enums, validation errors, Result
    Ingestion/              IngestionService, receipts, row results
    Sources/Harbour/        Harbour row class, CsvHelper map, adapter
    Responses/              Response file model and formatter
    Data/                   EF Core context and seed data
tests/
  TxnIngest.Core.Tests/     MSTest tests
```

## Tests

Tests use MSTest, and test method names start with `Test_`.
Each test uses its own in-memory SQLite database, so tests do not depend on the local `txningest.db`.
The clock is a `TimeProvider`, so tests can fix the current time.
