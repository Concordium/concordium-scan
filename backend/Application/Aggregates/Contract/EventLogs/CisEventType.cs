namespace Application.Aggregates.Contract.EventLogs
{
    /// <summary>
    /// Type of Cis Event <see href="https://proposals.concordium.software/CIS/cis-2.html#logged-events"/>
    /// </summary>
    public enum CisEventType
    {
        TokenMetadata = 251,
        UpdateOperator = 252,
        Burn = 253,
        Mint = 254,
        Transfer = 255,
    }
}