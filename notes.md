The zip file contains a small C# command line tool that ingests partner transaction files. Your task is to add support for a second partner. All the details are in TASK.md, and README.md covers how to build, test, and run it.

A few things to know: 

    You'll need the .NET 8 SDK, but there are no other external dependencies.
    Spend no more than 2 hours on it. We don't expect it to be perfect, and your NOTES.md is a good place to say what you'd do with more time.
    Use whatever tools you normally would, including AI assistants. Just be ready to explain the decisions you made, since you’ll walk through the code in the follow-up session.
    We're more interested in the decisions you made and process you followed than a perfect result.

Please send your solution back by Monday, October 26 at 5:00 pm PT, either as a zip with your commit history or a link to a git repo. 
You’ll walk through your solution in a follow-up session with Darryl, our COO, and Jon, our Director of Information Security. Are you available on Wednesday, October 28 at 3:00 pm PT for an hour?
If you have trouble getting the project to build or run, please let Tyler know and he'll help sort it out.

**********************************************************

First step is to make sure my environment can run it and I understand the basic code.

One of the best places to start is unit tests, since I can get a sense of what we're checking for.
    -> Got those running, everything passes.

If we're just adding a new platform / filetype, it'll end up looking fairly similar to the existing solution.

Going to look at both the old and new CSV files to get a sense of what's different.

Similar values, but different headers.
Comparing the two spec files to understand what's different. 
    -> Both are daily sends.
    -> transaction_id = ref (but different syntax / strings), merchant_id = mid, transaction_datetime = txn_date, amount = txn_amt, transaction_type = txn_code
    -> the final fields do not match / have analogues for each other but they do appear to both be 6 characters long.
    -> values are different enough that we're going to have to track which files the records are coming from if we're storing them all in one place.
        -> if other specs are also this similar it may be worth doing that.

    -> after checking the TransactionValues object it makes more sense to keep everything in the same object. UnmappedValues does a lot of heavy lifting.
        -> considering auth_code is already being placed in there, seems like this may be the intended solution.

    -> already have the sourceSystem enum too, definitely meant to be expanded out this way.

    