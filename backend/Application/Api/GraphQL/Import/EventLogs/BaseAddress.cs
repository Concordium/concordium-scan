namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represent parsed addeed from CIS event logs
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#address" />
    /// </summary>
    public abstract class BaseAddress
    {

        public BaseAddress(CisEventAddressType type)
        {
            this.Type = type;
        }

        public CisEventAddressType Type { get; private set; }
    }
}