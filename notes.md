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


Finally changing the code.
    -> First add that new enum, check where we reference Harbour.

    Data Section
        -> IngestCommand is where we actually pull things in, edit that next.
            -> Everything else in there looks fine.
        -> May want to keep an eye on SeedData later just in case we need it.
        -> UTC is pure util, doesn't matter.

    Domain Section
        -> Currency is going to be mildly troublesome, as we'll have to reference the merchant.
            -> Ask Maple if it could be added to the .csv?

        -> Transaction.cs contains some parsing logic, may need to be split by provider as they have different requirements.

        -> Maple spec mentions a third transaction type that's not currently present in TransactionType.cs, need to add void.

        -> Actual business logic is in the IngestionService.cs file, uses a HarbourAdapter.
            -> We want to mimic this design for Maple.

        -> First course of action is to mimic the Sources/Harbour implementation.
            -> Going to change class names in the MapleRow.cs file, as it'll be easier to work with when comparing to the CSV.
            
        Maple Map / Row / Adapter
            -> We can simply append the date and time later to combine them rather than immediately fusing them.
            -> There's no mention in the spec about what timezone it's in. Assume UTC, though I could see it potentially using the merchant like it does the currency.
            -> It might be worth moving some functions such as ReadDateTime into a shared space to save on code duplication, but in the interest of time I won't.
                -> Both adapters could inherit from some generic shared parent.

            -> Defaulting to CAD for now for Maple's currency, would need a proper implementation later.



THINGS TO KEEP IN MIND FOR MAPLE FROM SPEC:
    - All amounts are in the merchant's settlement currency.
        -> Will need to reference something to know if it's CAD or USD; or possibly add some kind of placeholder.

    - Maple may resend a transaction with the same `ref` if it was corrected.
        -> To me this means that the second / later transaction is the one that actually counts. Should replace the old one.