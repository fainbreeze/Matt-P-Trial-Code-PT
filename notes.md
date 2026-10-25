Hi! In this file I'll essentially be noting my thoughts as I worked through the assignment, with my closing remarks at the end. 
It may be a little hard to follow, but this is essentially how I take notes while I work daily (though in a little less detail).
The file may look a little weird on git, but the raw file should look okay.

***************
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

THINGS TO KEEP IN MIND FOR MAPLE FROM SPEC:
    - All amounts are in the merchant's settlement currency.
        -> Will need to reference something to know if it's CAD or USD; or possibly add some kind of placeholder.

    - Maple may resend a transaction with the same `ref` if it was corrected.
        -> To me this means that the second / later transaction is the one that actually counts. Should replace the old one.


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

            -> Defaulting to CAD for now for Maple's currency, would need a proper implementation later if it's not a purely CAD company / we have access to the merchants.

    Running the actual program outside tests:
        -> Ran it and it looks like harbour has not been broken, but maple isn't quite there yet.
        -> Currently parsing it as a harbour file, so we need to add some switches.
        -> Figured it small issue with the map! Fixed how it was reading from the .csv.

    Running into validation errors now such as OccurredAtUtc, looking at it now.
        -> realized the way I was combining the date and time values was slightly off, remedied.
        -> the main issue now is when we get a duplicate Id, as it should replace the old one rather than error out.

    The second Maple file appears to be reading 100450 twice due to the fact that the default seed has that transaction ID occupied already.
        -> May lead to more collisions in the future, could potentially be worth adding some kind of safeguard in the future.

    First File
        -> Going over errors to make things right. September 31 stumped me for a moment, but I see why it's erroring now.
        -> Fixed the error where 500102's replacement wouldn't be read by flipping the order of the list! 

************
    Ran out of time here. Next undoubtedly would have been writing unit tests alongside more polish.
    I would have referenced the existing tests for Harbour and most likely added more to the existing files for Maple specific things such as the unique properties and duplicate handling.
    There's one or two places I left a commented out Console.WriteLines as well, and while this would obviously not make it to the actual PR I left them in as this is a sort of WIP.
    I'd also have to contact someone about Maple's time and currency, as I had to assume both UTC and CAD for those fields. (Though I know currency looks at the merchant.)

    There's a few places I definitely could see code duplication being cut down, as the flow between the two providers is so similar.
    Alongside that, I'd like to have moved a few things to separate functions, as it makes the code look a lot cleaner.

    There's no doubt to me that writing more unit tests surely would have caught one or two bugs but in terms of where the program is at I'm fairly satisfied for the time put in.
    It seems to handle both the Maple files well and all of the old tests perform as they did before.

    Specific tests I would have written:
        -> Making sure values for one provider causes the other to error.
        -> Making sure void transactions are handled correctly.
        -> Making sure a third provider causes an error.
        -> Making sure the later entry of the same id is the one that's used for Maple csvs (since it's the correction).
        -> A suite of tests for each individual property for Maple.