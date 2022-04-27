create view graphql_baker_rewards as
select account_id as baker_id, index, time, entry_type as reward_type, amount, block_id    
from graphql_account_statement_entries
where entry_type >= 6