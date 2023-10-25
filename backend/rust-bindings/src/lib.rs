use std::{
    ffi::{CStr, CString},
    os::raw::c_char,
};

use anyhow::{anyhow, Result};
use concordium_contracts_common::{
    schema::{Type, VersionedModuleSchema},
    Cursor,
};
use serde_json::to_string;

pub type HexString = String;
pub type JsonString = String;

#[repr(C)]
pub struct FFIByteOption {
    pub t: u8,
    pub is_some: u8,
}

impl FFIByteOption {
    pub fn into_option(self) -> Option<u8> {
        match self.is_some {
            1 => Option::Some(self.t),
            _ => Option::None,
        }
    }
}

/// Get module schema in a human interpretable form.
///
/// # Arguments
///
/// * 'schema' - Module schema in hexadecimal
/// * 'schem_version' - Optinal schema version
/// * 'result' - Parsed schema if the call succeeded or the error message in case of failure.
///
/// # Returns
///
/// If the call succeeded or not.
///
/// # Safety
///
/// This function is marked as unsafe because it performs operations that are not checked by the
/// Rust compiler.
#[no_mangle]
pub unsafe extern "C" fn schema_display(
    schema: *const c_char,
    schema_version: FFIByteOption,
    result: *mut *mut c_char,
) -> bool {
    assign_result(result, || {
        let schema_hex = get_str_from_pointer(schema)?;
        schema_display_aux(schema_hex, schema_version.into_option())
    })
}

/// Get contract receive parameters in a human interpretable form.
///
/// Receive parameters are those given to a contract entrypoint on a update call.
///
/// # Arguments
///
/// * 'schema' - Module schema in hexadecimal
/// * 'schem_version' - Optinal schema version
/// * 'contract_name' - Contract name
/// * 'entrypoint' - Entrypoint of contract
/// * 'value' - Receive parameters in hexadecimal
/// * 'result' - Parsed receive parameters if the call succeeded or the error message in case of failure.
///
/// # Returns
///
/// If the call succeeded or not.
///
/// # Safety
///
/// This function is marked as unsafe because it performs operations that are not checked by the
/// Rust compiler.
#[no_mangle]
pub unsafe extern "C" fn get_receive_contract_parameter(
    schema: *const c_char,
    schema_version: FFIByteOption,
    contract_name: *const c_char,
    entrypoint: *const c_char,
    value: *const c_char,
    result: *mut *mut c_char,
) -> bool {
    assign_result(result, || {
        let schema_hex = get_str_from_pointer(schema)?;
        let contract_name_str = get_str_from_pointer(contract_name)?;
        let entrypoint_str = get_str_from_pointer(entrypoint)?;
        let value_hex = get_str_from_pointer(value)?;

        get_receive_contract_parameter_aux(
            schema_hex,
            schema_version.into_option(),
            &contract_name_str,
            &entrypoint_str,
            value_hex,
        )
    })
}

/// Get contract event in a human interpretable form.
///
/// # Arguments
///
/// * 'schema' - Module schema in hexadecimal
/// * 'schem_version' - Optinal schema version
/// * 'contract_name' - Contract name
/// * 'value' - Contract event in hexadecimal
/// * 'result' - Parsed contract event if the call succeeded or the error message in case of failure.
///
/// # Returns
///
/// If the call succeeded or not.
///
/// # Safety
///
/// This function is marked as unsafe because it performs operations that are not checked by the
/// Rust compiler.
#[no_mangle]
pub unsafe extern "C" fn get_event_contract(
    schema: *const c_char,
    schema_version: FFIByteOption,
    contract_name: *const c_char,
    value: *const c_char,
    result: *mut *mut c_char,
) -> bool {
    assign_result(result, || {
        let schema_hex = get_str_from_pointer(schema)?;
        let contract_name_str = get_str_from_pointer(contract_name)?;
        let value_hex = get_str_from_pointer(value)?;

        get_event_contract_aux(
            schema_hex,
            schema_version.into_option(),
            &contract_name_str,
            value_hex,
        )
    })
}

/// Compute result using the provided callback f, convert it into a C string and assign it to the provided target.
///
/// # Arguments
///
/// * 'target' - Pointer to a C String, which will be assigned the result / error message of f.
/// * 'f' - callback function, which result should be assigned to target.
///
/// # Returns
///
/// A boolean, that indicates whether the computation was successful or not.
///
/// # Safety
///
/// This function is marked as unsafe because it deferences a raw pointer.
unsafe fn assign_result<F: FnOnce() -> Result<T>, T: ToString>(
    target: *mut *mut c_char,
    f: F,
) -> bool {
    match f() {
        Ok(output) => {
            *target = CString::new(output.to_string()).unwrap().into_raw();
            true
        }
        Err(e) => {
            *target = CString::new(e.to_string()).unwrap().into_raw();
            false
        }
    }
}

pub fn get_receive_contract_parameter_aux(
    schema: HexString,
    schema_version: Option<u8>,
    contract_name: &str,
    entrypoint: &str,
    serialized_value: HexString,
) -> Result<String> {
    let module_schema = VersionedModuleSchema::new(&hex::decode(schema)?, &schema_version)?;
    let parameter_type = module_schema.get_receive_param_schema(contract_name, entrypoint)?;
    let deserialized = deserialize_type_value(serialized_value, &parameter_type, true)?;
    Ok(deserialized)
}

fn schema_display_aux(schema: HexString, schema_version: Option<u8>) -> Result<String> {
    let decoded = hex::decode(schema)?;
    let display = VersionedModuleSchema::new(&decoded, &schema_version)?;
    Ok(display.to_string())
}

fn get_event_contract_aux(
    schema: HexString,
    schema_version: Option<u8>,
    contract_name: &str,
    serialized_value: HexString,
) -> Result<String> {
    let module_schema = VersionedModuleSchema::new(&hex::decode(schema)?, &schema_version)?;
    let parameter_type = module_schema.get_event_schema(contract_name)?;
    let deserialized = deserialize_type_value(serialized_value, &parameter_type, true)?;
    Ok(deserialized)
}

fn deserialize_type_value(
    serialized_value: HexString,
    value_type: &Type,
    verbose_error_message: bool,
) -> Result<String> {
    let decoded = hex::decode(serialized_value)?;
    let mut cursor = Cursor::new(decoded);
    match value_type.to_json(&mut cursor) {
        Ok(v) => Ok(to_string(&v)?),
        Err(e) => Err(anyhow!("{}", e.display(verbose_error_message))),
    }
}

fn get_str_from_pointer(input: *const c_char) -> Result<String> {
    let c_str: &CStr = unsafe { CStr::from_ptr(input) };
    let str_slice: &str = c_str.to_str()?;
    Ok(str_slice.to_string())
}

#[cfg(test)]
mod test {
    use super::*;
    use std::fs;

    #[test]
    fn test_display_event() -> Result<()> {
        // Arrange
        let expected = r#"{"Mint":{"amount":"1000000","owner":{"Account":["3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV"]},"token_id":""}}"#;
        let schema_version = Option::None;
        let schema = fs::read_to_string("./test-data/cis2_wCCD_sub")?;
        let contract_name = "cis2_wCCD";
        let message =
            "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";

        // Act
        let display =
            get_event_contract_aux(schema, schema_version, contract_name, message.to_string())?;

        // Assert
        assert_eq!(display, expected);
        Ok(())
    }

    #[test]
    fn test_display_receive_param() -> Result<()> {
        // Arrange
        let expected = r#"{"data":"","to":{"Account":["3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV"]}}"#;
        let schema_version = Option::None;
        let schema = fs::read_to_string("./test-data/cis2_wCCD_sub")?;
        let contract_name = "cis2_wCCD";
        let entrypoint = "wrap";
        let message = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";

        // Act
        let display = get_receive_contract_parameter_aux(
            schema,
            schema_version,
            contract_name,
            entrypoint,
            message.to_string(),
        )?;

        // Assert
        assert_eq!(display, expected);
        Ok(())
    }

    #[test]
    fn test_display_module_schema() -> Result<()> {
        // Arrange
        let expected = r#"Contract: TestContract
  Event:
    {
      "Enum": [
        {
          "Foo": []
        },
        {
          "Bar": []
        }
      ]
    }
"#;
        let schema_version = Option::None;
        let schema = "ffff03010000000c00000054657374436f6e7472616374000000000001150200000003000000466f6f020300000042617202".to_string();

        // Act
        let display = schema_display_aux(schema, schema_version)?;

        // Assert
        assert_eq!(display, expected);
        Ok(())
    }
}
