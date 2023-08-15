namespace Application.Api.GraphQL.Import;

internal readonly record struct FinalizationTimeUpdate(long MinBlockHeight, long MaxBlockHeight);