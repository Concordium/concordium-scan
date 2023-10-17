using Application.Api.GraphQL.Accounts;

namespace Application.Aggregates.Contract.Types;

public readonly record struct ModuleReferenceEventInfo(
    ulong BlockHeight,
    string TransactionHash,
    ulong TransactionIndex,
    uint EventIndex,
    string ModuleReference,
    AccountAddress Sender,
    ImportSource Source,
    DateTimeOffset BlockSlotTime);
