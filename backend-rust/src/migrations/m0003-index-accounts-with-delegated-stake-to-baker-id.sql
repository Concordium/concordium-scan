-- Important for quickly calculating the delegated stake to a baker pool.
CREATE INDEX delegated_target_baker_id_index ON accounts(delegated_target_baker_id);
