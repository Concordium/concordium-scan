ALTER TABLE accounts
ADD COLUMN canonical_address CHAR(29) DEFAULT '0000000000000000000000000000000000000000';
CREATE INDEX accounts_base_address_idx ON accounts (canonical_address);
