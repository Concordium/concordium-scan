using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL;

public class Transaction
{
    [ID]
    public long Id { get; set; }
    
    [ID(nameof(Block))]
    public long BlockId { get; set; }
    
    public string BlockHash { get; set; }
    
    public long BlockHeight { get; set; }
    
    public int TransactionIndex { get; set; }
    
    public string TransactionHash { get; set; }
    
    public string? SenderAccountAddress { get; set; }
    
    public long CcdCost { get; set; }
    
    public long EnergyCost { get; set; }
    
    // TODO: how to model BlockItemKind and TransactionType? Strings, enums, other?

    // TODO: How to model outcome/result?
}
