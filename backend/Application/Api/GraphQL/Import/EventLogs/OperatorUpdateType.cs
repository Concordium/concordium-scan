namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Type of Operator Update Event
    /// <see cref="https://proposals.concordium.software/CIS/cis-2.html#updateoperator"/>
    /// </summary>
    public enum OperatorUpdateType
    {
        RemoveOperator = 0,
        AddOperator = 1
    }
}