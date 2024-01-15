namespace Application.Aggregates.Contract.EventLogs
{
    /// <summary>
    /// Types of Address in CIS Standards.
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#address"/>
    /// </summary>
    public enum CisEventAddressType
    {
        AccountAddress = 0,
        ContractAddress = 1,
    }
}