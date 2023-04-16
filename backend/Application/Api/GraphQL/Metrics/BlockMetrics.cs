using HotChocolate;

namespace Application.Api.GraphQL.Metrics;

public record BlockMetrics(
    [property:GraphQLDescription("The most recent block height. Equals the total length of the chain minus one (genesis block is at height zero).")]
    long LastBlockHeight,
    [property:GraphQLDescription("Total number of blocks added in requested period.")]
    int BlocksAdded, 
    [property:GraphQLDescription("The average block time (slot-time difference between two adjacent blocks) in the requested period. Will be null if no blocks have been added in the requested period.")]
    double? AvgBlockTime,
    [property:GraphQLDescription("The average finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the requested period. Will be null if no blocks have been finalized in the requested period.")]
    double? AvgFinalizationTime,
    [property:GraphQLDescription("The current total amount of CCD in existence.")]
    long LastTotalMicroCcd,
    [property:GraphQLDescription("The total CCD Released. This is total CCD supply not counting the balances of non circulating accounts")]
    long? LastTotalMicroCcdReleased,
    [property:GraphQLDescription("The current total CCD released according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule.")]
    long? LastTotalMicroCcdUnlocked,
    [property:GraphQLDescription("The current total amount of CCD in encrypted balances.")]
    long LastTotalMicroCcdEncrypted,
    [property:GraphQLDescription("The current total amount of CCD staked.")]
    long LastTotalMicroCcdStaked,
    [property:GraphQLDescription("The current percentage of CCD released (of total CCD in existence) according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule.")]
    double? LastTotalPercentageReleased,
    [property:GraphQLDescription("The current percentage of CCD encrypted (of total CCD in existence)")]
    double LastTotalPercentageEncrypted,
    [property:GraphQLDescription("The current percentage of CCD staked (of total CCD in existence)")]
    double LastTotalPercentageStaked,
    BlockMetricsBuckets Buckets);
    
public record BlockMetricsBuckets(
    [property:GraphQLDescription("The width (time interval) of each bucket.")]
    TimeSpan BucketWidth,
    
    [property:GraphQLDescription("Start of the bucket time period. Intended x-axis value.")]
    DateTimeOffset[] X_Time,
    
    [property:GraphQLDescription("Number of blocks added within the bucket time period. Intended y-axis value.")]
    int[] Y_BlocksAdded,
    
    [property:GraphQLDescription("The minimum block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period.")]
    double?[] Y_BlockTimeMin,
    
    [property:GraphQLDescription("The average block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period.")]
    double?[] Y_BlockTimeAvg,
    
    [property:GraphQLDescription("The maximum block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period.")]
    double?[] Y_BlockTimeMax,
    
    [property:GraphQLDescription("The minimum finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the bucket period. Intended y-axis value. Will be null if no blocks have been finalized in the bucket period.")]
    double?[] Y_FinalizationTimeMin,
    
    [property:GraphQLDescription("The average finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the bucket period. Intended y-axis value. Will be null if no blocks have been finalized in the bucket period.")]
    double?[] Y_FinalizationTimeAvg,
    
    [property:GraphQLDescription("The maximum finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the bucket period. Intended y-axis value. Will be null if no blocks have been finalized in the bucket period.")]
    double?[] Y_FinalizationTimeMax,

    [property:GraphQLDescription("The total amount of CCD in existence at the end of the bucket period. Intended y-axis value.")]
    long[] Y_LastTotalMicroCcd,
    
    [property:GraphQLDescription("The minimum amount of CCD in encrypted balances in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period.")]
    long?[] Y_MinTotalMicroCcdEncrypted,
    
    [property:GraphQLDescription("The maximum amount of CCD in encrypted balances in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period.")]
    long?[] Y_MaxTotalMicroCcdEncrypted,
    
    [property:GraphQLDescription("The total amount of CCD in encrypted balances at the end of the bucket period. Intended y-axis value.")]
    long[] Y_LastTotalMicroCcdEncrypted,
    
    [property:GraphQLDescription("The minimum amount of CCD staked in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period.")]
    long[] Y_MinTotalMicroCcdStaked,
    
    [property:GraphQLDescription("The maximum amount of CCD staked in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period.")]
    long[] Y_MaxTotalMicroCcdStaked,
    
    [property:GraphQLDescription("The total amount of CCD staked at the end of the bucket period. Intended y-axis value.")]
    long[] Y_LastTotalMicroCcdStaked);
