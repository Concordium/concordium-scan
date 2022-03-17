create table graphql_chain_parameters
(
    id                             int primary key generated always as identity,
    election_difficulty            numeric not null,
    euro_per_energy_numerator      bigint  not null,
    euro_per_energy_denominator    bigint  not null,
    micro_ccd_per_euro_numerator   bigint  not null,
    micro_ccd_per_euro_denominator bigint  not null,
    baker_cooldown_epochs          bigint  not null,
    credentials_per_block_limit    bigint  not null,
    mint_mint_per_slot             numeric not null,
    mint_baking_reward             numeric not null,
    mint_finalization_reward       numeric not null,
    tx_fee_baker                   numeric not null,
    tx_fee_gas_account             numeric not null,
    gas_baker                      numeric not null,
    gas_finalization_proof         numeric not null,
    gas_account_creation           numeric not null,
    gas_chain_update               numeric not null,
    foundation_account_id          bigint  not null,
    foundation_account_address     text    not null,
    minimum_threshold_for_baking   bigint  not null
);

