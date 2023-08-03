/*
 Change 'election_difficulty' and 'gas_finalization_proof' column to allow null since 'ChainParametersV2' used from 
 protocol 6 doesn't have these parameters.
 */
ALTER TABLE graphql_chain_parameters 
ALTER COLUMN election_difficulty DROP NOT NULL,
ALTER COLUMN gas_finalization_proof DROP NOT NULL;