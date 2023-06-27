using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Import;

public record TransactionPair (
    BlockItemSummary Source,
    Transaction Target);