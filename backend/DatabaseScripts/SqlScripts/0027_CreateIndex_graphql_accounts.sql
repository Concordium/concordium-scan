create index graphql_accounts_delegation_target_baker_id_index
    on graphql_accounts (delegation_target_baker_id, delegation_staked_amount desc)
    where delegation_target_baker_id is not null