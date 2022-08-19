namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents Account Address in types of addresses a CIS token standard uses. 
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#address"/>
    /// </summary>
    public class CisEventAddressAccount : BaseAddress
    {
        public CisEventAddressAccount() : base(CisEventAddressType.AccountAddress)
        {
        }

        public ConcordiumSdk.Types.AccountAddress Address { get; set; }
    }
}