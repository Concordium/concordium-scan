namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents Contract Address in types of addresses a CIS token standard uses
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#address"/>
    /// </summary>
    public class CisEventAddressContract : BaseAddress
    {
        public CisEventAddressContract() : base(CisEventAddressType.ContractAddress)
        {
        }

        public ulong Index { get; set; }
        public ulong SubIndex { get; set; }
    }
}