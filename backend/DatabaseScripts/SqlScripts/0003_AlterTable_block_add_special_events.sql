alter table block
    add column mint_baking_reward               bigint null, 
    add column mint_finalization_reward         bigint null, 
    add column mint_platform_development_charge bigint null,
    add column mint_foundation_account          bytea null,
    add column block_reward_transaction_fees    bigint null,
    add column block_reward_old_gas_account     bigint null,
    add column block_reward_new_gas_account     bigint null,
    add column block_reward_baker_reward        bigint null,
    add column block_reward_foundation_charge   bigint null,
    add column block_reward_baker_address       bytea null,
    add column block_reward_foundation_account  bytea null,
    add column finalization_reward_remainder    bigint null,
    add column baking_reward_remainder          bigint null;