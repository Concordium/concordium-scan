/*
Add columns for current payday commissions.

Values are initialized to those of the latest change to commissions received. They may not be correct since changes
on the payday of the migration will be wrongly used.

The values will be reset on the next payday block, hence the values will only be wrong until the next payday block
is received.
*/
ALTER TABLE graphql_bakers
ADD COLUMN active_pool_payday_status_transaction_commission     numeric null,
ADD COLUMN active_pool_payday_status_finalization_commission    numeric null,
ADD COLUMN active_pool_payday_status_baking_commission          numeric null;

UPDATE graphql_bakers
SET 
    active_pool_payday_status_transaction_commission = active_pool_transaction_commission,
    active_pool_payday_status_finalization_commission = active_pool_finalization_commission,
    active_pool_payday_status_baking_commission = active_pool_baking_commission;
