use crate::{
    address::{Address, ContractAddress},
    graphql_api::{get_pool, ApiError, ApiResult},
    scalar_types::Amount,
};
use async_graphql::{connection, ComplexObject, Context, Enum, SimpleObject};
use concordium_rust_sdk::base::{
    contracts_common::{
        schema::{Type, VersionedModuleSchema},
        Cursor,
    },
    smart_contracts::ReceiveName,
};

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
pub enum ContractVersion {
    V0,
    V1,
}

#[derive(Debug, thiserror::Error, Clone)]
#[error("Invalid contract version: {0}")]
pub struct InvalidContractVersionError(i32);

impl TryFrom<i32> for ContractVersion {
    type Error = InvalidContractVersionError;

    fn try_from(value: i32) -> Result<Self, Self::Error> {
        match value {
            0 => Ok(ContractVersion::V0),
            1 => Ok(ContractVersion::V1),
            _ => Err(InvalidContractVersionError(value)),
        }
    }
}

impl From<concordium_rust_sdk::types::smart_contracts::WasmVersion> for ContractVersion {
    fn from(value: concordium_rust_sdk::types::smart_contracts::WasmVersion) -> Self {
        use concordium_rust_sdk::types::smart_contracts::WasmVersion;
        match value {
            WasmVersion::V0 => ContractVersion::V0,
            WasmVersion::V1 => ContractVersion::V1,
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractModuleDeployed {
    pub module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct ContractInitialized {
    pub module_ref:        String,
    pub contract_address:  ContractAddress,
    pub amount:            Amount,
    pub init_name:         String,
    pub version:           ContractVersion,
    // All logged events by the smart contract during the transaction execution.
    pub contract_logs_raw: Vec<Vec<u8>>,
}

#[ComplexObject]
impl ContractInitialized {
    async fn events_as_hex(&self) -> ApiResult<connection::Connection<String, String>> {
        let mut connection = connection::Connection::new(true, true);

        self.contract_logs_raw.iter().enumerate().for_each(|(index, log)| {
            connection.edges.push(connection::Edge::new(index.to_string(), hex::encode(log)));
        });

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }

    async fn events<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<connection::Connection<String, String>> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the event schema if it exists.
        let opt_event_schema = if let Some(schema) = row.display_schema.as_ref() {
            let versioned_schema = VersionedModuleSchema::new(schema, &None).map_err(|_| {
                ApiError::InternalError(
                    "Database bytes should be a valid VersionedModuleSchema".to_string(),
                )
            })?;

            versioned_schema.get_event_schema(&row.contract_name).ok()
        } else {
            None
        };

        let mut connection = connection::Connection::new(true, true);

        for (index, log) in self.contract_logs_raw.iter().enumerate() {
            let decoded_log = decode_value_with_schema(
                opt_event_schema.as_ref(),
                log,
                SmartContractSchemaNames::Event,
            );

            connection.edges.push(connection::Edge::new(index.to_string(), decoded_log));
        }

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct ContractUpdated {
    pub contract_address:  ContractAddress,
    pub instigator:        Address,
    pub amount:            Amount,
    pub receive_name:      String,
    pub version:           ContractVersion,
    // All logged events by the smart contract during this section of the transaction execution.
    pub contract_logs_raw: Vec<Vec<u8>>,
    pub input_parameter:   Vec<u8>,
}

#[ComplexObject]
impl ContractUpdated {
    async fn message_as_hex(&self) -> ApiResult<String> { Ok(hex::encode(&self.input_parameter)) }

    async fn message<'a>(&self, ctx: &Context<'a>) -> ApiResult<String> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the receive param schema if it exists.
        let opt_receive_param_schema = if let Some(schema) = row.display_schema.as_ref() {
            let versioned_schema = VersionedModuleSchema::new(schema, &None).map_err(|_| {
                ApiError::InternalError(
                    "Database bytes should be a valid VersionedModuleSchema".to_string(),
                )
            })?;

            versioned_schema
                .get_receive_param_schema(
                    &row.contract_name,
                    ReceiveName::new_unchecked(&self.receive_name).entrypoint_name().into(),
                )
                .ok()
        } else {
            None
        };

        let decoded_input_parameter = decode_value_with_schema(
            opt_receive_param_schema.as_ref(),
            &self.input_parameter,
            SmartContractSchemaNames::InputParameterReceiveFunction,
        );

        Ok(decoded_input_parameter)
    }

    async fn events_as_hex(&self) -> ApiResult<connection::Connection<String, String>> {
        let mut connection = connection::Connection::new(true, true);

        self.contract_logs_raw.iter().enumerate().for_each(|(index, log)| {
            connection.edges.push(connection::Edge::new(index.to_string(), hex::encode(log)));
        });

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }

    async fn events<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<connection::Connection<String, String>> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the event schema if it exists.
        let opt_event_schema = if let Some(schema) = row.display_schema.as_ref() {
            let versioned_schema = VersionedModuleSchema::new(schema, &None).map_err(|_| {
                ApiError::InternalError(
                    "Database bytes should be a valid VersionedModuleSchema".to_string(),
                )
            })?;

            versioned_schema.get_event_schema(&row.contract_name).ok()
        } else {
            None
        };

        let mut connection = connection::Connection::new(true, true);

        for (index, log) in self.contract_logs_raw.iter().enumerate() {
            let decoded_log = decode_value_with_schema(
                opt_event_schema.as_ref(),
                log,
                SmartContractSchemaNames::Event,
            );

            connection.edges.push(connection::Edge::new(index.to_string(), decoded_log));
        }

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractCall {
    pub contract_updated: ContractUpdated,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct ContractInterrupted {
    pub contract_address:  ContractAddress,
    // All logged events by the smart contract during this section of the transaction execution.
    pub contract_logs_raw: Vec<Vec<u8>>,
}

#[ComplexObject]
impl ContractInterrupted {
    async fn events_as_hex(&self) -> ApiResult<connection::Connection<String, String>> {
        let mut connection = connection::Connection::new(true, true);

        self.contract_logs_raw.iter().enumerate().for_each(|(index, log)| {
            connection.edges.push(connection::Edge::new(index.to_string(), hex::encode(log)));
        });

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }

    async fn events<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<connection::Connection<String, String>> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the event schema if it exists.
        let opt_event_schema = if let Some(schema) = row.display_schema.as_ref() {
            let versioned_schema = VersionedModuleSchema::new(schema, &None).map_err(|_| {
                ApiError::InternalError(
                    "Database bytes should be a valid VersionedModuleSchema".to_string(),
                )
            })?;

            versioned_schema.get_event_schema(&row.contract_name).ok()
        } else {
            None
        };

        let mut connection = connection::Connection::new(true, true);

        for (index, log) in self.contract_logs_raw.iter().enumerate() {
            let decoded_log = decode_value_with_schema(
                opt_event_schema.as_ref(),
                log,
                SmartContractSchemaNames::Event,
            );

            connection.edges.push(connection::Edge::new(index.to_string(), decoded_log));
        }

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractResumed {
    pub contract_address: ContractAddress,
    pub success:          bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractUpgraded {
    pub contract_address: ContractAddress,
    pub from:             String,
    pub to:               String,
}

struct SchemaName {
    type_name:  &'static str,
    value_name: &'static str,
}

enum SmartContractSchemaNames {
    Event,
    InputParameterReceiveFunction,
}

impl SmartContractSchemaNames {
    pub const EVENT: SchemaName = SchemaName {
        type_name:  "event",
        value_name: "contract log",
    };
    pub const INPUT_PARAMETER_RECEIVE_FUNCTION: SchemaName = SchemaName {
        type_name:  "receive parameter",
        value_name: "input parameter of receive function",
    };

    pub fn value(&self) -> &'static str {
        match self {
            SmartContractSchemaNames::Event => Self::EVENT.value_name,
            SmartContractSchemaNames::InputParameterReceiveFunction => {
                Self::INPUT_PARAMETER_RECEIVE_FUNCTION.value_name
            }
        }
    }

    pub fn kind(&self) -> &'static str {
        match self {
            SmartContractSchemaNames::Event => Self::EVENT.type_name,
            SmartContractSchemaNames::InputParameterReceiveFunction => {
                Self::INPUT_PARAMETER_RECEIVE_FUNCTION.type_name
            }
        }
    }
}

fn decode_value_with_schema(
    opt_schema: Option<&Type>,
    value: &[u8],
    schema_name: SmartContractSchemaNames,
) -> String {
    let Some(schema) = opt_schema else {
        // Note: There could be something better displayed than this string if no schema
        // is available for decoding at the frontend long-term.
        return format!(
            "No embedded {} schema in smart contract available for decoding",
            schema_name.kind()
        );
    };

    let mut cursor = Cursor::new(&value);
    match schema.to_json(&mut cursor) {
        Ok(v) => {
            serde_json::to_string(&v).unwrap_or_else(|e| {
                // We don't return an error here since the query is correctly formed and
                // the CCDScan backend is working as expected.
                // A wrong/missing schema is a mistake by the smart contract
                // developer which in general cannot be fixed after the deployment of
                // the contract. We display the error message (instead of the decoded
                // value) in the block explorer to make the info visible to the smart
                // contract developer for debugging purposes here.
                format!(
                    "Failed to deserialize {} with {} schema into string: {:?}",
                    schema_name.value(),
                    schema_name.kind(),
                    e
                )
            })
        }
        Err(e) => {
            // We don't return an error here since the query is correctly formed and
            // the CCDScan backend is working as expected.
            // A wrong/missing schema is a mistake by the smart contract
            // developer which in general cannot be fixed after the deployment of
            // the contract. We display the error message (instead of the decoded
            // value) in the block explorer to make the info visible to the smart
            // contract developer for debugging purposes here.
            format!(
                "Failed to deserialize {} with {} schema: {:?}",
                schema_name.value(),
                schema_name.kind(),
                e.display(true)
            )
        }
    }
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "module_reference_contract_link_action")]
pub enum ModuleReferenceContractLinkAction {
    Added,
    Removed,
}
