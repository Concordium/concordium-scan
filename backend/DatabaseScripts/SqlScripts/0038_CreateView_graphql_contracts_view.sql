ALTER TABLE graphql_transaction_events
	ALTER COLUMN event SET DATA type jsonb;
CREATE INDEX event_tag on graphql_transaction_events (((event->>'tag')::INTEGER));
CREATE INDEX event_contract_init_contract_address ON graphql_transaction_events (((event->'data'->>'ContractAddress'))) WHERE (event->>'tag')::INTEGER = 16;
CREATE INDEX event_contract_upgrade_contract_address ON graphql_transaction_events (((event->'data'->>'ContractAddress'))) WHERE (event->>'tag')::INTEGER = 36;
CREATE INDEX event_contract_update_contract_address ON graphql_transaction_events (((event->'data'->>'ContractAddress'))) WHERE (event->>'tag')::INTEGER = 18;
CREATE INDEX event_contract_transfer_from_contract_address ON graphql_transaction_events (((event->'data'->'From'->>'data'))) WHERE (event->>'tag')::Integer = 1 AND (event->'data'->'From'->>'tag')::Integer = 2;
CREATE OR REPLACE VIEW graphql_contracts_view 
AS
	WITH contract_init_events AS (
	SELECT
		transaction_id,
		index,
		(event->'data'->>'ContractAddress') as contract_address,
		(event->'data'->>'ModuleRef') as module_ref,
		(event->'data'->>'Amount')::BIGINT as amount_in
	FROM graphql_transaction_events 
	WHERE (event->>'tag')::Integer = 16
	),
	contract_upgrade_events AS (
		SELECT
			transaction_id,
			index,
			(event->'data'->>'ContractAddress') as contract_address,
			(event->'data'->>'From') as from_module_ref,
			(event->'data'->>'To') as to_module_ref
		FROM graphql_transaction_events
		WHERE (event->>'tag')::Integer = 36
	),
	contract_update_events AS (
			SELECT
			transaction_id,
			index,
			(event->'data'->>'ContractAddress') as contract_address,
			(event->'data'->>'Amount')::BIGINT as amount_in
		FROM graphql_transaction_events
		WHERE (event->>'tag')::Integer = 18
	),
	contract_transfer_events AS (
		SELECT
			transaction_id,
			index,
			(event->'data'->'From'->>'data') as contract_address,
			(event->'data'->>'Amount')::BIGINT as amount_out
		FROM graphql_transaction_events
		WHERE (event->>'tag')::Integer = 1 AND (event->'data'->'From'->>'tag')::Integer = 2
	),
	contract_module AS (
		SELECT contract_address, module_ref, first_transaction_id, last_transaction_id, transactions_count FROM (
			SELECT 
				FIRST_VALUE(transaction_id) OVER W1 AS first_transaction_id, 
				LAST_VALUE(transaction_id) OVER W1 AS last_transaction_id, 
				contract_address, 
				module_ref,
				COUNT(transaction_id) over W1 AS transactions_count,
				rank() OVER W1 as rn
			FROM (
				SELECT transaction_id, index, contract_address, module_ref FROM contract_init_events
				UNION ALL 
				SELECT transaction_id, index, contract_address, to_module_ref as module_ref FROM contract_upgrade_events
			) e1
			WINDOW w1 AS (PARTITION BY contract_address ORDER BY transaction_id DESC)
		) e2
		WHERE rn = 1
	),
	contract_balances AS (
		SELECT 
			contract_address, 
			SUM(amount_in) - SUM(amount_out) AS balance, 
			MAX(transaction_id) AS last_transaction_id, 
			MIN(transaction_id) AS first_transaction_id,
			COUNT(DISTINCT transaction_id) AS transactions_count
		FROM (
			SELECT transaction_id, index, contract_address, amount_in, 0 AS amount_out FROM contract_update_events
			UNION ALL
			SELECT transaction_id, index, contract_address, amount_in, 0 AS amount_out FROM contract_init_events
			UNION ALL
			SELECT transaction_id, index, contract_address, 0 AS amount_in, amount_out FROM contract_transfer_events
		) balance_events
		GROUP BY contract_address
	),
	contracts_temp AS (
		SELECT 
			contract_address, 
			module_ref,
			balance,
			LEAST(contract_module.first_transaction_id, contract_balances.first_transaction_id) AS first_transaction_id,
			GREATEST(contract_module.last_transaction_id, contract_balances.last_transaction_id) AS last_transaction_id,
			contract_module.transactions_count + contract_balances.transactions_count - 1 AS transactions_count
		FROM contract_module 
		LEFT JOIN contract_balances USING (contract_address)
	), contracts AS (
		SELECT
			graphql_transactions.id AS id,
			contracts_temp.contract_address,
			contracts_temp.module_ref,
			contracts_temp.balance,
			contracts_temp.first_transaction_id,
			contracts_temp.last_transaction_id,
			contracts_temp.transactions_count,
			graphql_transactions.sender AS owner,
			graphql_blocks.block_slot_time AS created_time
		FROM contracts_temp 
		JOIN graphql_transactions ON contracts_temp.first_transaction_id = graphql_transactions.id
		JOIN graphql_blocks ON graphql_transactions.block_id = graphql_blocks.id
		ORDER BY first_transaction_id
	)
SELECT * FROM contracts;