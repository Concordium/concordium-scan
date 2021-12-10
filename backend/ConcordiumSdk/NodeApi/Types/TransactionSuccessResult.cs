using System.Text.Json;

namespace ConcordiumSdk.NodeApi.Types;

/// <summary>
/// Events still not deserialized to strongly type events.
/// Should probably be done at some time, something like this...
/// 
/// public class TransactionResultEvent
/// {
/// }
///
/// public class Transferred : TransactionResultEvent
/// {
///     public string Amount { get; init; }
///     public AddressWithType To { get; init; }
///     public AddressWithType From { get; init; }
/// }
///
/// public class AddressWithType
/// {
///     public string Address { get; init; }
///     public string Type { get; init; }
/// }
/// </summary>
public class TransactionSuccessResult : TransactionResult
{
    public JsonElement Events { get; init; }
}