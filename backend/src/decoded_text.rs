use async_graphql::{Enum, SimpleObject};

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DecodedText {
    text:        String,
    decode_type: TextDecodeType,
}

impl DecodedText {
    /// Attempt to parse the bytes as a CBOR string otherwise use HEX to present
    /// the bytes.
    pub fn from_bytes(bytes: &[u8]) -> Self {
        if let Ok(text) = ciborium::from_reader::<String, _>(bytes) {
            Self {
                text,
                decode_type: TextDecodeType::Cbor,
            }
        } else {
            Self {
                text:        hex::encode(bytes),
                decode_type: TextDecodeType::Hex,
            }
        }
    }
}

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
pub enum TextDecodeType {
    Cbor,
    Hex,
}
