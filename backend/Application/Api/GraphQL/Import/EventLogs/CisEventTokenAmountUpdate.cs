using System.Numerics;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents that an a CIS token was updated.
    /// </summary>
    public class CisEventTokenAmountUpdate : CisEventTokenUpdate
    {
        public BigInteger AmountDelta { get; set; }
    }
}