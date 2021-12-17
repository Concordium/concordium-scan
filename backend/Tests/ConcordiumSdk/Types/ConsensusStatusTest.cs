using System.Text.Json;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types.JsonConverters;

namespace Tests.ConcordiumSdk.Types;

public class ConsensusStatusTest
{
    /// <summary>
    /// This is a test of a hard-to-come-by scenario:
    /// Deserialization of the response for the initial consensus status (before the node has had the time to sync any data from peers)
    /// The JSON used has been captured from a node so represents actual data.
    /// </summary>
    [Fact]
    public void Deserialize_InitialConsensusStatus()
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        jsonSerializerOptions.Converters.Add(new BlockHashConverter());

        var json = "{\"bestBlock\":\"b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d\",\"genesisBlock\":\"b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d\",\"genesisTime\":\"2021-05-07T12:00:00Z\",\"slotDuration\":250,\"epochDuration\":3600000,\"lastFinalizedBlock\":\"b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d\",\"bestBlockHeight\":0,\"lastFinalizedBlockHeight\":0,\"blocksReceivedCount\":0,\"blockLastReceivedTime\":null,\"blockReceiveLatencyEMA\":0.0,\"blockReceiveLatencyEMSD\":0.0,\"blockReceivePeriodEMA\":null,\"blockReceivePeriodEMSD\":null,\"blocksVerifiedCount\":0,\"blockLastArrivedTime\":null,\"blockArriveLatencyEMA\":0.0,\"blockArriveLatencyEMSD\":0.0,\"blockArrivePeriodEMA\":null,\"blockArrivePeriodEMSD\":null,\"transactionsPerBlockEMA\":0.0,\"transactionsPerBlockEMSD\":0.0,\"finalizationCount\":0,\"lastFinalizedTime\":null,\"finalizationPeriodEMA\":null,\"finalizationPeriodEMSD\":null,\"protocolVersion\":1,\"genesisIndex\":0,\"currentEraGenesisBlock\":\"b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d\",\"currentEraGenesisTime\":\"2021-05-07T12:00:00Z\"}";
        var result = JsonSerializer.Deserialize<ConsensusStatus>(json, jsonSerializerOptions);
        Assert.NotNull(result);
    }
}