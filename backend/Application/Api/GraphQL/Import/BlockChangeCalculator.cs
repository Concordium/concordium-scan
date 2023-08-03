using Application.NodeApi;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Import;

public class BlockChangeCalculator
{
    private readonly IInitialTokenReleaseScheduleRepository _repository;

    public BlockChangeCalculator(IInitialTokenReleaseScheduleRepository repository)
    {
        _repository = repository;
    }

    public ulong? CalculateTotalAmountUnlocked(DateTimeOffset blockSlotTime, string genesisBlockHash)
    {
        var isMainnet = ConcordiumNetworkId.Mainnet.GenesisBlockHash == BlockHash.From(genesisBlockHash);
        if (!isMainnet) 
            return null;
        
        var releaseScheduleItem = _repository.GetMainnetSchedule()
            .LastOrDefault(x => x.Timestamp <= blockSlotTime);
        
        return releaseScheduleItem != null
            ? releaseScheduleItem.Amount
            : null;
    }
}
