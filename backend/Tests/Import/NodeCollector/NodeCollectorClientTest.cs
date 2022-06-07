using System.Net.Http;
using Application.Import.NodeCollector;
using FluentAssertions;

namespace Tests.Import.NodeCollector;

public class NodeCollectorClientTest
{
    private readonly NodeCollectorClient _target;

    public NodeCollectorClientTest()
    {
        var settings = new NodeCollectorClientSettings
        {
            Address = "https://dashboard.mainnet.concordium.software/nodesSummary"
            // Address = "https://dashboard.testnet.concordium.com/nodesSummary"
        };
        _target = new NodeCollectorClient(settings, new HttpClient());
    }

    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetNodeSummaries()
    {
        var result = await _target.GetNodeSummaries();
        result.Should().NotBeNull();
    }
}