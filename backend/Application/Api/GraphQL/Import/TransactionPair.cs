using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.Import;

public record TransactionPair (
    TransactionSummary Source,
    Transaction Target);