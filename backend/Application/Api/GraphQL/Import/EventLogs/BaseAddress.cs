namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represent parsed addeed from CIS event logs
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#address" />
    /// </summary>
    public abstract class BaseAddress
    {
        protected BaseAddress(CisEventAddressType type)
        {
            Type = type;
        }

        public CisEventAddressType Type { get; private set; }
    }
}
