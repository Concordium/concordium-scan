ALTER TABLE accounts
    ALTER COLUMN canonical_address SET NOT NULL,
    ADD CONSTRAINT canonical_address_unique UNIQUE (canonical_address)
    ADD CONSTRAINT check_canonical_address_length CHECK (LENGTH(canonical_address) = 29);
