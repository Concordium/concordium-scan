using Application.Import;

namespace Application.Api.GraphQL.Import;

public class ImportState
{
    public int Id { get; private set; }
    public string GenesisBlockHash { get; set; }
    public long MaxImportedBlockHeight { get; set; }
    public long CumulativeAccountsCreated { get; set; }
    public long CumulativeTransactionCount { get; set; }
    public DateTimeOffset LastBlockSlotTime { get; set; }
    public long MaxBlockHeightWithUpdatedFinalizationTime { get; set; }
    public DateTimeOffset? NextPendingBakerChangeTime { get; set; }
    public ChainParameters? LatestWrittenChainParameters { get; set; }
    public int LastGenesisIndex { get; set; }
    public int TotalBakerCount { get; set; }

    public static ImportState CreateGenesisState(GenesisBlockDataPayload payload)
    {
        return new ImportState
        {
            GenesisBlockHash = payload.BlockInfo.BlockHash.AsString,
            MaxImportedBlockHeight = 0,
            CumulativeAccountsCreated = 0,
            CumulativeTransactionCount = 0,
            LastBlockSlotTime = payload.BlockInfo.BlockSlotTime,
            MaxBlockHeightWithUpdatedFinalizationTime = -1,
            NextPendingBakerChangeTime = null,
            LatestWrittenChainParameters = null,
            LastGenesisIndex = 0,
            TotalBakerCount = 0
        };
    }

    public void CopyValuesFrom(ImportState source)
    {
        Id = source.Id;
        GenesisBlockHash = source.GenesisBlockHash;
        MaxImportedBlockHeight = source.MaxImportedBlockHeight;
        CumulativeAccountsCreated = source.CumulativeAccountsCreated;
        CumulativeTransactionCount = source.CumulativeTransactionCount;
        LastBlockSlotTime = source.LastBlockSlotTime;
        MaxBlockHeightWithUpdatedFinalizationTime = source.MaxBlockHeightWithUpdatedFinalizationTime;
        NextPendingBakerChangeTime = source.NextPendingBakerChangeTime;
        LatestWrittenChainParameters = source.LatestWrittenChainParameters;
        LastGenesisIndex = source.LastGenesisIndex;
        TotalBakerCount = source.TotalBakerCount;
    }
}