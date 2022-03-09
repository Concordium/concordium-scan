﻿using Application.Import;

namespace Application.Api.GraphQL.EfCore;

public class ImportState
{
    public int Id { get; private set; }
    public string GenesisBlockHash { get; set; }
    public long MaxImportedBlockHeight { get; set; }
    public long CumulativeAccountsCreated { get; set; }
    public long CumulativeTransactionCount { get; set; }
    public DateTimeOffset LastBlockSlotTime { get; set; }
    public long MaxBlockHeightWithUpdatedFinalizationTime { get; set; }

    public static ImportState CreateGenesisState(GenesisBlockDataPayload payload)
    {
        return new ImportState
        {
            GenesisBlockHash = payload.BlockInfo.BlockHash.AsString,
            MaxImportedBlockHeight = 0,
            CumulativeAccountsCreated = 0,
            CumulativeTransactionCount = 0,
            LastBlockSlotTime = payload.BlockInfo.BlockSlotTime,
            MaxBlockHeightWithUpdatedFinalizationTime = -1
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
    }
}