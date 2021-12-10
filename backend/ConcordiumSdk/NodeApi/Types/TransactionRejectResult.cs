namespace ConcordiumSdk.NodeApi.Types;

/// <summary>
/// This type does not reflect the full set of data available.
///
/// As can be seen in the Haskell source (https://github.com/Concordium/concordium-base/blob/a50612e023da79cb625cd36c52703af6ed483738/haskell-src/Concordium/Types/Execution.hs#L1034)
/// there is a finite set of reject reasons, modelled individually to allow reject reasons
/// to carry state that wary by reject reason.
///
/// However, the serialized JSON simply contains this state in one array, fx:
/// {
///    "tag": "AmountTooLarge",
///    "contents": [
///    {
///        "type": "AddressAccount",
///        "address": "3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH"
///    },
///    "5000000000"
///    ]
/// }
///
/// As can be seen there are no keys/fields for the data in contents, so what does "5000000000" actually represent?
///
/// Implementation of a proper deserialization has been postponed for now :)  
/// </summary>
public class TransactionRejectResult : TransactionResult
{
    public string Tag { get; init; }
}