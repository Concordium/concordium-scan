use crate::aux_functions::{deserialize_recieve_message_aux};
use interoptopus::ffi_function;
use interoptopus::patterns::{option::FFIOption, string::AsciiPointer};
use std::ffi::{c_char, CString};

#[ffi_function]
#[no_mangle]
/// Deserialize a recieve message.
pub extern "C" fn deserialize_recieve_message(
    return_value_bytes: AsciiPointer,
    module_schema: AsciiPointer,
    contract_name: AsciiPointer,
    function_name: AsciiPointer,
    schema_version: FFIOption<u8>,
) -> *const c_char {
    let return_value_bytes = return_value_bytes.as_str().unwrap();
    let module_schema = module_schema.as_str().unwrap();
    let contract_name = contract_name.as_str().unwrap();
    let function_name = function_name.as_str().unwrap();

    let s = match deserialize_recieve_message_aux(
        return_value_bytes.to_string(),
        module_schema.to_string(),
        contract_name,
        function_name,
        schema_version.into_option(),
    ) {
        Ok(s) => s,
        Err(e) => format!("{}", e),
    };

    CString::new(s).unwrap().into_raw()
}