# Task: Add Maple Card Services files to TxnIngest

## Background

TxnIngest is a command line tool for Ledgerline, a payment processor.
Partners send us daily files of card transactions.
TxnIngest reads a file, validates each row, stores the good rows, and produces a response file.
The response file tells the partner which rows we accepted or rejected, and why.

Today TxnIngest supports one partner: Harbour Bank.
We signed a second partner: Maple Card Services.

## Your task

Add support for Maple Card Services transaction files.
The spec is in `docs/maple-file-spec.md`.
Sample files are in `samples/maple/`.

When you are done:

- `txningest ingest --source maple --file <path>` ingests a Maple file.
- `txningest receipts show <token>` shows a correct response for a Maple file.
- Harbour ingestion still works, and all existing tests pass.
- New behaviour has tests.
- `NOTES.md` lists the assumptions you made, the questions you would ask Maple, and what you would change with more time.

## Ground rules

- Time box: 2 hours.
- You may use any tools, including AI assistants.
- If you use AI, be ready to explain every line you submit.
- Submit a git repository or a zip with your commit history.

## What we look at

We read your code, your tests, and your `NOTES.md`.
We care about clarity, correctness, and how well you explain your choices.
Then we talk about your work in a review session.

See `README.md` for how to build, test, and run the tool.
