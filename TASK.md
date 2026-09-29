# Task: Add Maple Card Services files to TxnIngest

## Background

The TxnIngest project is a command line tool for a fictional payment processor, Ledgerline. 

The tool is used to ingest transaction files from partners and produce response files for them. Files are sent daily to 
be ingested by the Ledgerline system.

TxnIngest is responsible for reading each file, validating each row, storing the good rows, and producing a response file.
The response file tells the partner which rows we accepted or rejected, and why.

Today TxnIngest project supports one partner: Harbour Bank.
In this scenario, we will add a second partner: Maple Card Services.

## Your task

Your task is to add support for the Maple Card Services transaction files. File specs are in `docs/maple-file-spec.md`
and sample files are in `samples/maple`.

Acceptance Criteria:

- `txningest ingest --source maple --file <path>` ingests a Maple file.
- `txningest receipts show <token>` shows a correct response for a Maple file.
- Harbour ingestion still works, and all existing tests pass.
- New behaviour has test coverage.
- Provide a `NOTES.md` file which lists the assumptions you made, the questions you would ask Maple, and what you would change with more time.

## Ground rules

- Time box: Don't spend more than 2 hours.
- You may use any tools, including AI coding assistants. If you use AI, be prepared to explain every line you submit.
- Any IDE or command line tool may be used, but if you don't have an IDE setup you can use VS Code with the C# Dev Kit extension.
- Submit a git repository or a zip with your commit history.

## What we look at

- We will review your code in a PR Review style for clarity and correctness.
- We will review the `NOTES.md` file that you provide to understand what tradeoffs you made.
- In the meeting, we will discuss your code and the decisions you made in implementing the new behaviour.

See `README.md` for how to build, test, and run the tool.
