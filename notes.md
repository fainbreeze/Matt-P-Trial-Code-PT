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

