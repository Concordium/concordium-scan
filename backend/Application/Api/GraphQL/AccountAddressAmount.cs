using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL;

public record AccountAddressAmount(
    AccountAddress AccountAddress,
    ulong Amount);