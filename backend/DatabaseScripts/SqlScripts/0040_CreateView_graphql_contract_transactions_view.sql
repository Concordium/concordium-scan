CREATE INDEX event_contract_interrupted_contract_address ON graphql_transaction_events (((event->'data'->>'ContractAddress'))) WHERE (event->>'tag')::INTEGER = 34;
CREATE INDEX event_contract_resumed_contract_address ON graphql_transaction_events (((event->'data'->>'ContractAddress'))) WHERE (event->>'tag')::INTEGER = 35;

ALTER TABLE graphql_transactions ALTER COLUMN reject_reason SET DATA type jsonb;
CREATE INDEX contract_rejected_transaction 
	ON graphql_transactions (((reject_reason->'data'->>'ContractAddress'))) 
	WHERE (reject_reason->>'tag')::Integer = 13 AND transaction_type='0.2';

CREATE OR REPLACE VIEW public.graphql_contract_transactions_view
AS
WITH
contract_init_events AS (
	SELECT
		transaction_id,
		index,
		(event->'data'->>'ContractAddress') as contract_address
	FROM graphql_transaction_events 
	WHERE (event->>'tag')::Integer = 16
),
contract_upgrade_events AS (
	SELECT
		transaction_id,
		index,
		(event->'data'->>'ContractAddress') as contract_address
	FROM graphql_transaction_events
	WHERE (event->>'tag')::Integer = 36
),
contract_update_events AS (
		SELECT
		transaction_id,
		index,
		(event->'data'->>'ContractAddress') as contract_address
	FROM graphql_transaction_events
	WHERE (event->>'tag')::Integer = 18
),
contract_transfer_events AS (
	SELECT
		transaction_id,
		index,
		(event->'data'->'From'->>'data') as contract_address
	FROM graphql_transaction_events
	WHERE (event->>'tag')::Integer = 1 AND (event->'data'->'From'->>'tag')::Integer = 2
),
contract_interrupted_events AS (
	SELECT
		transaction_id,
		index,
		(event->'data'->>'ContractAddress') as contract_address
	FROM graphql_transaction_events
	WHERE (event->>'tag')::Integer = 34
),
contract_resumed_events AS (
	SELECT
		transaction_id,
		index,
		(event->'data'->>'ContractAddress') as contract_address
	FROM graphql_transaction_events
	WHERE (event->>'tag')::Integer = 35
),
contract_rejected_events AS (
	SELECT
		id AS transaction_id,
		index,
		(reject_reason->'data'->>'ContractAddress') as contract_address
	FROM graphql_transactions
	WHERE (reject_reason->>'tag')::Integer = 13 AND transaction_type='0.2'
),
all_events AS (
	SELECT * FROM contract_init_events
	UNION ALL 
	SELECT * FROM contract_upgrade_events
	UNION ALL
	SELECT * FROM contract_update_events
	UNION ALL
	SELECT * FROM contract_transfer_events
	UNION ALL
	SELECT * FROM contract_interrupted_events
	UNION ALL
	SELECT * FROM contract_resumed_events
	UNION ALL
	SELECT * from contract_rejected_events
)

SELECT transaction_id, contract_address
FROM all_events
GROUP BY transaction_id, contract_address
