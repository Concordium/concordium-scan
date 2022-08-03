namespace Application.Api.GraphQL.Import.EventLogs
{
    public interface IEventLogWriter
    {
        void ApplyTokenUpdates(IEnumerable<CisEventTokenUpdate> tokenUpdates);
        void ApplyAccountUpdates(IEnumerable<CisAccountUpdate> accountUpdates);
    }
}