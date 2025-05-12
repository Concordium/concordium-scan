-- Add a text version of the index column for partial searches.
ALTER TABLE contracts
ADD COLUMN index_text TEXT GENERATED ALWAYS AS (index::text) STORED;

-- Used to efficiently perform partial string matching on the contracts.index column,
-- allowing fast lookups when searching for contracts by their index prefix using `starts_with`.
CREATE INDEX partial_contracts_index_idx ON contracts USING btree (index_text text_pattern_ops);


