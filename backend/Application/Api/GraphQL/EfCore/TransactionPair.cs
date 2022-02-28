using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.EfCore;

public record TransactionPair (
    TransactionSummary Source,
    Transaction Target);