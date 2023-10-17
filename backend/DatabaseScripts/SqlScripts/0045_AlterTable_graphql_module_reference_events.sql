ALTER TABLE graphql_module_reference_events
ADD COLUMN module_source    text    null,
ADD COLUMN schema           text    null,
ADD COLUMN schema_version   int     null;
