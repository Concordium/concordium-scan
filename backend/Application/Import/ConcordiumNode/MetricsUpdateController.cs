using System.Threading.Tasks;
using Application.Api.GraphQL;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Npgsql;

namespace Application.Import.ConcordiumNode;

public class MetricsUpdateController
{
    private readonly DatabaseSettings _settings;
    private BlockInfo? _prevBlockInfo;
    private long? _cumulativeTransactionCount;
    private long? _cumulativeAccountsCreated;

    public MetricsUpdateController(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task BlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary, AccountInfo[] createdAccounts, RewardStatus rewardStatus)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();
        
        var tx = await conn.BeginTransactionAsync();

        await InsertBlockMetrics(blockInfo, rewardStatus, conn);
        var newCumulativeTransactionCount = await InsertTransactionMetrics(blockInfo, blockSummary, conn);
        var newCumulativeAccountsCreated = await InsertAccountsMetrics(blockInfo, createdAccounts, conn);
        
        await tx.CommitAsync();
        
        _cumulativeTransactionCount = newCumulativeTransactionCount;
        _cumulativeAccountsCreated = newCumulativeAccountsCreated;
        
        _prevBlockInfo = blockInfo;
    }

    private async Task<long?> InsertTransactionMetrics(BlockInfo blockInfo, BlockSummary blockSummary, NpgsqlConnection conn)
    {
        if (blockSummary.TransactionSummaries.Length == 0) return _cumulativeTransactionCount;

        var cumulativeTransactionCount = _cumulativeTransactionCount;
        if (!cumulativeTransactionCount.HasValue)
        {
            var initSql = @"select max(cumulative_transaction_count)
                            from metrics_transactions
                            where time = (select max(time) from metrics_transactions)";
            var maxCumulativeTxCount = await conn.QuerySingleOrDefaultAsync<long?>(initSql);
            cumulativeTransactionCount = maxCumulativeTxCount ?? 0;
        }

        var transactionParams = blockSummary.TransactionSummaries.Select((txs, ix) => new
        {
            CumulativeTransactionCount = cumulativeTransactionCount.Value + ix + 1,
            Time = blockInfo.BlockSlotTime,
            TransactionType = TransactionTypeUnion.CreateFrom(txs.Type).ToCompactString(),
            MicroCcdCost = Convert.ToInt64(txs.Cost.MicroCcdValue),
            Success = txs.Result is TransactionSuccessResult
        }).ToArray();
        
        var sql = "insert into metrics_transactions (time, cumulative_transaction_count, transaction_type, micro_ccd_cost, success) values (@Time, @CumulativeTransactionCount, @TransactionType, @MicroCcdCost, @Success)";
        await conn.ExecuteAsync(sql, transactionParams);

        return cumulativeTransactionCount.Value + blockSummary.TransactionSummaries.Length;
    }

    private async Task InsertBlockMetrics(BlockInfo blockInfo, RewardStatus rewardStatus, NpgsqlConnection conn)
    {
        var blockParam = new
        {
            Time = blockInfo.BlockSlotTime,
            blockInfo.BlockHeight,
            BlockTimeSecs = GetBlockTime(blockInfo),
            TotalMicroCcd = (long)rewardStatus.TotalAmount.MicroCcdValue,
            TotalEncryptedMicroCcd = (long)rewardStatus.TotalEncryptedAmount.MicroCcdValue
            
        };

        var sql = @"insert into metrics_block (time, block_height, block_time_secs, total_microccd, total_encrypted_microccd) 
                    values (@Time, @BlockHeight, @BlockTimeSecs, @TotalMicroCcd, @TotalEncryptedMicroCcd)";
        await conn.ExecuteAsync(sql, blockParam);
    }

    private double GetBlockTime(BlockInfo blockInfo)
    {
        if (_prevBlockInfo == null)
            return 0; // TODO: Should only be valid for genesis block, but right now every time the app starts the first imported data item will be zero. Will be corrected later...
        var blockTime = blockInfo.BlockSlotTime - _prevBlockInfo.BlockSlotTime;
        return Math.Round(blockTime.TotalSeconds, 1);
    }

    private async Task<long?> InsertAccountsMetrics(BlockInfo blockInfo, AccountInfo[] createdAccounts, NpgsqlConnection conn)
    {
        if (createdAccounts.Length == 0) return _cumulativeAccountsCreated;

        var cumulativeAccountsCreated = _cumulativeAccountsCreated;
        if (!cumulativeAccountsCreated.HasValue)
        {
            var initSql = @"select max(cumulative_accounts_created)
                            from metrics_accounts
                            where time = (select max(time) from metrics_accounts)";
            var maxTotalAccounts = await conn.QuerySingleOrDefaultAsync<long?>(initSql);
            cumulativeAccountsCreated = maxTotalAccounts ?? 0;
        }
        
        var accountsParams = new
        {
            Time = blockInfo.BlockSlotTime,
            CumulativeAccountsCreated = cumulativeAccountsCreated.Value + createdAccounts.Length,
            AccountsCreated = createdAccounts.Length
        };
        
        var sql = "insert into metrics_accounts (time, cumulative_accounts_created, accounts_created) values (@Time, @CumulativeAccountsCreated, @AccountsCreated)";
        await conn.ExecuteAsync(sql, accountsParams);

        return cumulativeAccountsCreated;
    }
}