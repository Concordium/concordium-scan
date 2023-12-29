namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Base class representing a CIS token update to be applied to the database.
    /// The properties defined in the class can uniquely identity a Contract on chain. 
    /// </summary>
    public abstract class CisEventTokenUpdate
    {
        /// <summary>
        /// Contract Index of the CIS Token.
        /// </summary>
        public ulong ContractIndex { get; set; }

        /// <summary>
        /// Contract subindex of the CIS Token.
        /// </summary>
        public ulong ContractSubIndex { get; set; }

        /// <summary>
        /// Id of the CIS Token.
        /// </summary>
        public string TokenId { get; set; }
    }
}
