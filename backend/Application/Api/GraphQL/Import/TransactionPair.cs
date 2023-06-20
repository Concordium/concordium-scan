using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.Import;

public record TransactionPair (
    TransactionSummary Source,
    Transaction Target);