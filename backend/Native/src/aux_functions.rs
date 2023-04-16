use anyhow::{anyhow, Result};
use concordium_base::contracts_common::{schema::VersionedModuleSchema, Cursor};

/// Deserialize a recieve message.
pub fn deserialize_recieve_message_aux(
    recieve_value_bytes: String,
    schema: String,
    contract_name: &str,
    function_name: &str,
    schema_version: Option<u8>,
) -> Result<String> {
    let module_schema = VersionedModuleSchema::new(&hex::decode(schema)?, &schema_version)?;
    let recieve_param_schema =
        module_schema.get_receive_param_schema(contract_name, function_name)?;
    let mut rv_cursor = Cursor::new(hex::decode(recieve_value_bytes)?);
    match recieve_param_schema.to_json(&mut rv_cursor) {
        Ok(rv) => Ok(rv.to_string()),
        Err(_) => Err(anyhow!("Unable to parse return value to json.")),
    }
}
