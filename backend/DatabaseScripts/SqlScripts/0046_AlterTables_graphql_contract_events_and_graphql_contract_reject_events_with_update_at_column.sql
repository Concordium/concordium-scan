ALTER TABLE graphql_contract_events
    ADD COLUMN updated_at TIMESTAMPTZ     null;
ALTER TABLE graphql_contract_reject_events
    ADD COLUMN updated_at TIMESTAMPTZ     null;
