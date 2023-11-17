ALTER TABLE graphql_bakers
ADD COLUMN active_pool_payday_status_transaction_commission     numeric null,
ADD COLUMN active_pool_payday_status_finalization_commission    numeric null,
ADD COLUMN active_pool_payday_status_baking_commission          numeric null;
    
UPDATE graphql_bakers
SET 
    active_pool_payday_status_transaction_commission = (SELECT active_pool_transaction_commission from graphql_bakers),
    active_pool_payday_status_finalization_commission = (SELECT active_pool_finalization_commission from graphql_bakers),
    active_pool_payday_status_baking_commission = (SELECT active_pool_baking_commission from graphql_bakers);
