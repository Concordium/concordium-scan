/*
Add column for token address which consist of contract index, contract subindex and token id.

Adds index on the column since it would be needed for searching.
 */
ALTER TABLE graphql_tokens
ADD COLUMN token_address text null;

CREATE INDEX graphql_tokens_token_address_index
    ON graphql_tokens (token_address);
