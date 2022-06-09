using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Import.NodeCollector;

public class NodeCollectorClient
{
    private readonly NodeCollectorClientSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public NodeCollectorClient(NodeCollectorClientSettings settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<NodeSummary[]> GetNodeSummaries(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStringAsync(_settings.Address, cancellationToken);
        var nodeSummaries = JsonSerializer.Deserialize<NodeSummary[]>(response, _jsonOptions);
        return nodeSummaries ?? Array.Empty<NodeSummary>();
    }
}