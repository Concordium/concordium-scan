using Application.Api.GraphQL.Transactions;
using Application.NodeApi;
using Concordium.Sdk.Types.New;

namespace Application.Api.GraphQL.Import;

public record TransactionPair (
    TransactionSummary Source,
    Transaction Target);