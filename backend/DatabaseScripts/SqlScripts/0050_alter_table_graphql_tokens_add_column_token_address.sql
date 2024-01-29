/*
Add column for token address which consist of contract index, contract subindex and token id. 
 */
ALTER TABLE graphql_tokens
ADD COLUMN token_address text null;
