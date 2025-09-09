-- This index was added in migration file 0001, but due to a later migration script (0037) - dropping of the column 'type_account' inherently causes the index to also drop. THis index needs to be re-added here
-- Important for quickly filtering transactions related to a baker_id.
CREATE INDEX IF NOT EXISTS baker_related_tx_idx ON transactions (sender_index, type_account, index) WHERE type_account IN ('AddBaker', 'RemoveBaker', 'UpdateBakerStake', 'UpdateBakerRestakeEarnings', 'UpdateBakerKeys', 'ConfigureBaker');
