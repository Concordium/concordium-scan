use std::{os::raw::c_char, ffi::{CStr, CString}};

use anyhow::{Result, anyhow};
use concordium_base::contracts_common::{
        schema::{Type, VersionedModuleSchema}, Cursor,
    };
use hex;
use serde_json::to_string;

pub type HexString = String;
pub type JsonString = String;

#[repr(C)]
pub struct FFIOption {
    pub t: u8,
    pub is_some: u8
}

impl FFIOption {
    pub fn into_option(self) -> Option<u8> {
        match self.is_some {
            1 => Option::Some(self.t),
            _ => Option::None
        }
    }
}

#[no_mangle]
pub extern "C" fn test_option(
    schema_version: FFIOption,
) -> *const c_char {
    let mut schema_concat = "".to_string();
    if let Some(option) = schema_version.into_option() {
        schema_concat = format!("{schema_concat}-{option}")
    } else {
        schema_concat = format!("{schema_concat}-none")
    }
    CString::new(schema_concat).unwrap().into_raw()    
} 

#[no_mangle]
pub extern "C" fn schema_display(
    schema: *const c_char,
    schema_version: FFIOption,
    result: *mut *mut c_char) -> bool {
    
    let schema_hex = match get_str_from_pointer(schema, result) {
        Ok(out) => out,
        Err(_) => return false,
    };

    let display = match schema_display_aux(schema_hex, schema_version.into_option()) {
        Ok(display) => display,
        Err(e) => {
            unsafe {
                *result = CString::new(e.to_string()).unwrap().into_raw()    
            }
            return false
        }
    };
    unsafe {
        *result = CString::new(display.to_string()).unwrap().into_raw()
    }

    true
}

#[no_mangle]
pub extern "C" fn get_receive_contract_parameter(
    schema: *const c_char,
    schema_version: FFIOption,
    contract_name: *const c_char,
    entrypoint: *const c_char,
    value: *const c_char,
    result: *mut *mut c_char) -> bool {
    
    let schema_hex = match get_str_from_pointer(schema, result) {
        Ok(out) => out,
        Err(_) => return false,
    };
    let contract_name_str = match get_str_from_pointer(contract_name, result) {
        Ok(out) => out,
        Err(_) => return false,
    };
    let entrypoint_str = match get_str_from_pointer(entrypoint, result) {
        Ok(out) => out,
        Err(_) => return false,
    };
    let value_hex = match get_str_from_pointer(value, result) {
        Ok(out) => out,
        Err(_) => return false,
    };

    let display = match get_receive_contract_parameter_aux(schema_hex, schema_version.into_option(), &contract_name_str, &entrypoint_str, value_hex) {
        Ok(display) => display,
        Err(e) => {
            unsafe {
                *result = CString::new(e.to_string()).unwrap().into_raw()    
            }
            return false
        }
    };
    unsafe {
        *result = CString::new(display.to_string()).unwrap().into_raw()
    }
    true
}

#[no_mangle]
pub extern "C" fn get_event_contract(
    schema: *const c_char,
    schema_version: FFIOption,
    contract_name: *const c_char,
    value: *const c_char,
    result: *mut *mut c_char) -> bool {
    
    let schema_hex = match get_str_from_pointer(schema, result) {
        Ok(out) => out,
        Err(_) => return false,
    };
    let contract_name_str = match get_str_from_pointer(contract_name, result) {
        Ok(out) => out,
        Err(_) => return false,
    };
    let value_hex = match get_str_from_pointer(value, result) {
        Ok(out) => out,
        Err(_) => return false,
    };

    let display = match get_event_contract_aux(schema_hex, schema_version.into_option(), &contract_name_str, value_hex) {
        Ok(display) => display,
        Err(e) => {
            unsafe {
                *result = CString::new(e.to_string()).unwrap().into_raw()    
            }
            return false
        }
    };
    unsafe {
        *result = CString::new(display.to_string()).unwrap().into_raw()
    }
    true
}

pub fn schema_display_aux(schema: HexString, schema_version: Option<u8>) -> Result<String> {
    let decoded = hex::decode(schema)?;
    let display = VersionedModuleSchema::new(&decoded, &schema_version)?;
    Ok(display.to_string())
}

pub fn get_receive_contract_parameter_aux(
    schema: HexString,
    schema_version: Option<u8>,
    contract_name: &str,
    entrypoint: &str,
    serialized_value: HexString
) -> Result<String> {
    let module_schema = VersionedModuleSchema::new(&hex::decode(schema)?, &schema_version)?;
    let parameter_type = module_schema.get_receive_param_schema(contract_name, entrypoint)?;
    let deserialized = deserialize_type_value(serialized_value, &parameter_type, true)?;
    Ok(deserialized)
}

pub fn get_event_contract_aux(
    schema: HexString,
    schema_version: Option<u8>,
    contract_name: &str,
    serialized_value: HexString    
) -> Result<String> {
    let module_schema = VersionedModuleSchema::new(&hex::decode(schema)?, &schema_version)?;
    let parameter_type = module_schema.get_event_schema(contract_name)?;
    let deserialized = deserialize_type_value(serialized_value, &parameter_type, true)?;
    Ok(deserialized)
}

fn deserialize_type_value(
    serialized_value: HexString,
    value_type: &Type,
    verbose_error_message: bool
) -> Result<String> {
    let decoded = hex::decode(serialized_value)?;
    let mut cursor = Cursor::new(decoded);
    match value_type.to_json(&mut cursor) {
        Ok(v) => Ok(to_string(&v)?),
        Err(e) => Err(anyhow!("{}", e.display(verbose_error_message))),
    }
}

fn get_str_from_pointer(input: *const c_char, result: *mut *mut c_char) -> Result<String> {
    let c_str: &CStr = unsafe { CStr::from_ptr(input) };
    let str_slice: &str = match c_str.to_str() {
        Ok(r) => r,
        Err(e) => {
            unsafe {
                *result = CString::new(e.to_string()).unwrap().into_raw()
            }
            return Err(anyhow!("not able to parse pointer to string"))
        },
    };
    Ok(str_slice.to_string())
}


#[cfg(test)]
mod test {
    use std::fs;
    use super::*;

    #[test]
    fn test_display_event() -> Result<()> {
        // Arrange
        let expected = r#"{"Mint":{"amount":"1000000","owner":{"Account":["3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV"]},"token_id":""}}"#;
        let schema_version = Option::None;
        let schema = fs::read_to_string("./test-data/cis2_wCCD_sub")?;
        let contract_name = "cis2_wCCD";
        let message = "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";

        // Act
        let display = get_event_contract_aux(schema, schema_version, contract_name, message.to_string())?;

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
        let display = get_receive_contract_parameter_aux(schema, schema_version, contract_name, entrypoint, message.to_string())?;

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

    #[test]
    fn test_display_module_versioned_schema() -> Result<()> {
        // Arrange
        let _expected = r#"Contract: TestContract
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
        let schema_version = Option::Some(1);
        let schema = fs::read_to_string("./test-data/cis2-nft-schema")?;

        // Act
        let display = schema_display_aux(schema, schema_version)?;

        // Assert
        print!("{}", display);
        // assert_eq!(display, expected);
        Ok(())
    }    
}
