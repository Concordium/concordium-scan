using ConcordiumSdk.Types;

namespace Application.Api.GraphQL.Import;

public class BlockChangeCalculator
{
    private readonly IInitialTokenReleaseScheduleRepository _repository;

    public BlockChangeCalculator(IInitialTokenReleaseScheduleRepository repository)
    {
        _repository = repository;
    }

    public ulong? CalculateTotalAmountReleased(CcdAmount totalAmount, DateTimeOffset blockSlotTime, string genesisBlockHash)
    {
        var isMainnet = ConcordiumNetworkId.Mainnet.GenesisBlockHash == new BlockHash(genesisBlockHash);
        if (!isMainnet) 
            return null;
        
        var releaseScheduleItem = _repository.GetMainnetSchedule()
            .LastOrDefault(x => x.Timestamp <= blockSlotTime);
        
        return releaseScheduleItem != null
            ? totalAmount.MicroCcdValue - releaseScheduleItem.Amount
            : null;
    }
}