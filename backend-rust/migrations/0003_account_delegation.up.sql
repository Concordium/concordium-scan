-- Add up migration script here
ALTER TABLE accounts
ADD COLUMN delegated_restake_earnings BOOLEAN NULL,
ADD COLUMN delegated_target_baker_id BIGINT NULL