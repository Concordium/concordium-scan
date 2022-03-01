using HotChocolate.Types;

namespace Application.Api.GraphQL.Search;

[ExtendObjectType(typeof(Query))]
public class SearchQuery
{
    public SearchResult Search(string query)
    {
        return new SearchResult(query);
    }
}