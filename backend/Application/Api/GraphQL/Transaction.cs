namespace Application.Api.GraphQL;

public class Transaction
{
    public int Id { get; set; }
    public string BlockHash { get; set; }
    public int BlockHeight { get; set; }
    public int TransactionIndex { get; set; }
    public string TransactionHash { get; set; }
    public string SenderAccountAddress { get; set; }
    public int CcdCost { get; set; }
    public int EnergyCost { get; set; }
    
    // TODO: how to model BlockItemKind and TransactionType? Strings, enums, other?

    // TODO: How to model outcome/result?
}
