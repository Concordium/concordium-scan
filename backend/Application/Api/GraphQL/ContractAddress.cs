namespace Application.Api.GraphQL;

public class ContractAddress : Address
{
    public ContractAddress(ulong index, ulong subIndex)
    {
        Index = index;
        SubIndex = subIndex;
    }

    public ulong Index { get; }

    public ulong SubIndex { get; }
}