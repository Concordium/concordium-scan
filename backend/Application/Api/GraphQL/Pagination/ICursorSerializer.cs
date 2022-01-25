namespace Application.Api.GraphQL.Pagination;

public interface ICursorSerializer
{
    string Serialize(long value);
    long Deserialize(string serializedValue);
}