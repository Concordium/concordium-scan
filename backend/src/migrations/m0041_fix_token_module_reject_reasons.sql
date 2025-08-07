-- Migration to fix previously stored token module reject reasons
-- The old code was using serde_json::to_value(details) directly
-- Now it properly serializes using TokenModuleRejectReasonType

-- Create a function to migrate token module reject reasons
CREATE OR REPLACE FUNCTION migrate_token_module_reject_reason(old_reject_reason JSONB) 
RETURNS JSONB AS $$
DECLARE
    token_id TEXT;
    reason_type TEXT;
    details JSONB;
    old_details JSONB;
    new_reject_reason JSONB;
BEGIN
    -- Extract the token_id and reason_type from the old format
    IF old_reject_reason ? 'TokenUpdateTransactionFailed' THEN
        token_id := old_reject_reason->'TokenUpdateTransactionFailed'->>'token_id';
        reason_type := old_reject_reason->'TokenUpdateTransactionFailed'->>'reason_type';
        details := old_reject_reason->'TokenUpdateTransactionFailed'->'details';
        
        -- Check if this is already in the new format (has 'type' field in details)
        IF details ? 'type' THEN
            RETURN old_reject_reason;
        END IF;
        
        -- Extract the actual details from the nested structure based on reason_type
        old_details := details->reason_type;
        
        -- Build the new properly structured reject reason based on reason_type
        new_reject_reason := jsonb_build_object(
            'TokenUpdateTransactionFailed',
            jsonb_build_object(
                'token_id', token_id,
                'reason_type', reason_type,
                'details', CASE 
                    WHEN LOWER(reason_type) = 'addressnotfound' THEN
                        jsonb_build_object(
                            'type', 'AddressNotFound',
                            'index', COALESCE(old_details->>'index', '0'),
                            'address', CASE 
                                WHEN old_details->'address'->'type' = '"account"' THEN
                                    jsonb_build_object(
                                        'account', 
                                        jsonb_build_object(
                                            'address', 
                                            jsonb_build_object('as_string', old_details->'address'->>'address'),
                                            'coin_info',
                                            jsonb_build_object('coin_info_code', '919')
                                        )
                                    )
                                ELSE old_details->'address'
                            END
                        )
                    WHEN LOWER(reason_type) = 'tokenbalanceinsufficient' THEN
                        jsonb_build_object(
                            'type', 'TokenBalanceInsufficient',
                            'index', COALESCE(old_details->>'index', '0'),
                            'available_balance', jsonb_build_object(
                                'value', old_details->'availableBalance'->>'value',
                                'decimals', old_details->'availableBalance'->>'decimals'
                            ),
                            'required_balance', jsonb_build_object(
                                'value', old_details->'requiredBalance'->>'value',
                                'decimals', old_details->'requiredBalance'->>'decimals'
                            )
                        )
                    WHEN LOWER(reason_type) = 'deserializationfailure' THEN
                        jsonb_build_object(
                            'type', 'DeserializationFailure',
                            'cause', old_details->>'cause'
                        )
                    WHEN LOWER(reason_type) = 'unsupportedoperation' THEN
                        jsonb_build_object(
                            'type', 'UnsupportedOperation',
                            'index', COALESCE(old_details->>'index', '0'),
                            'operation_type', COALESCE(old_details->>'operationType', 'unknown'),
                            'reason', old_details->>'reason'
                        )
                    WHEN LOWER(reason_type) = 'operationnotpermitted' THEN
                        jsonb_build_object(
                            'type', 'OperationNotPermitted',
                            'index', COALESCE(old_details->>'index', '0'),
                            'address', CASE 
                                WHEN old_details->'address' IS NULL THEN NULL
                                WHEN old_details->'address'->'type' = '"account"' THEN
                                    jsonb_build_object(
                                        'account', 
                                        jsonb_build_object(
                                            'address', 
                                            jsonb_build_object('as_string', old_details->'address'->>'address'),
                                            'coin_info',
                                            jsonb_build_object('coin_info_code', '919')
                                        )
                                    )
                                ELSE old_details->'address'
                            END,
                            'reason', old_details->>'reason'
                        )
                    WHEN LOWER(reason_type) = 'mintwouldoverflow' THEN
                        jsonb_build_object(
                            'type', 'MintWouldOverflow',
                            'index', COALESCE(old_details->>'index', '0'),
                            'requested_amount', old_details->'requestedAmount',
                            'current_supply', old_details->'currentSupply',
                            'max_representable_amount', old_details->'maxRepresentableAmount'
                        )
                    ELSE
                        jsonb_build_object(
                            'type', 'Unknown',
                            'message', 'Unknown reject reason for type: ' || reason_type || '. Original details: ' || old_details::text
                        )
                END
            )
        );
        
        RETURN new_reject_reason;
    ELSE
        -- Return the original if it's not a TokenUpdateTransactionFailed
        RETURN old_reject_reason;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Update all transactions with TokenUpdateTransactionFailed reject reasons
DO $$
DECLARE
    batch_size INTEGER := 10000;
    total_updated INTEGER := 0;
    min_id BIGINT := -1;
    max_id BIGINT;
    rows_updated INTEGER;
BEGIN
    -- Get the maximum transaction index to know when to stop
    SELECT COALESCE(MAX(index), 0) INTO max_id FROM transactions 
    WHERE reject IS NOT NULL 
    AND reject ? 'TokenUpdateTransactionFailed'
    AND NOT (
        reject->'TokenUpdateTransactionFailed'->'details' ? 'type'
    ); -- Only count records that need migration
    
    RAISE NOTICE 'Starting migration of token module reject reasons. Processing transaction indexes from % to %', min_id, max_id;

    WHILE min_id < max_id LOOP
        UPDATE transactions 
        SET reject = migrate_token_module_reject_reason(reject)
        WHERE index > min_id 
        AND index <= min_id + batch_size
        AND reject IS NOT NULL 
        AND reject ? 'TokenUpdateTransactionFailed'
        AND NOT (
            reject->'TokenUpdateTransactionFailed'->'details' ? 'type'
        ); -- Only update if the details don't already have the 'type' field (new format)

        GET DIAGNOSTICS rows_updated = ROW_COUNT;
        total_updated := total_updated + rows_updated;
        min_id := min_id + batch_size;
        
        RAISE NOTICE 'Updated % rows in this batch. Total updated: %. Progress: %/%', 
                    rows_updated, total_updated, min_id, max_id;

        -- Optional: pause to reduce load
        PERFORM pg_sleep(0.01);
    END LOOP;

    RAISE NOTICE 'Finished migrating token module reject reasons. Total rows updated: %', total_updated;
END $$;

-- Clean up the temporary function
DROP FUNCTION migrate_token_module_reject_reason(JSONB);
