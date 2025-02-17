ALTER TABLE accounts
ADD COLUMN base_address BYTEA DEFAULT E'\\x0000000000000000000000000000000000000000',
ADD CONSTRAINT check_base_address_length CHECK (length(base_address) = 29);
CREATE INDEX accounts_base_address_idx ON accounts (base_address);
