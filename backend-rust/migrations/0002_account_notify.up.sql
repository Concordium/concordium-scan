CREATE OR REPLACE FUNCTION account_updated_notify_trigger_function() RETURNS trigger AS $trigger$
DECLARE
  rec affected_accounts;
  lookup_result TEXT;
BEGIN
  CASE TG_OP
       WHEN 'INSERT' THEN
            -- Lookup the account address associated with the account index.
            SELECT address
            INTO lookup_result
            FROM accounts
            WHERE index = NEW.account_index;

            -- Include the lookup result in the payload
            PERFORM pg_notify('account_updated', lookup_result);
       ELSE NULL;
  END CASE;
  RETURN NEW;
END;
$trigger$ LANGUAGE plpgsql;

CREATE TRIGGER account_updated_notify_trigger AFTER INSERT
ON affected_accounts
FOR EACH ROW EXECUTE PROCEDURE account_updated_notify_trigger_function();
