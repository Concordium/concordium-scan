namespace Application.Api.GraphQL;

public class BlockStatistics
{
    /// <summary>
    /// Number of seconds between block slot time of this block and previous block.
    /// </summary>
    public double BlockTime { get; set; }
    /// <summary>
    /// Number of seconds between the block slot time of this block and the block containing the finalization proof for this block.
    /// 
    /// This is an objective measure of the finalization time (determined by chain data alone) and will
    /// at least be the block time (currently on average 10s). The actual finalization time will usually be lower than
    /// that (currently 1-2s) but can only be determined in a subjective manner by each node: That is the time a
    /// node has first seen a block finalized. This is defined as the difference between when a finalization proof is
    /// first constructed, and the block slot time. However the time when a finalization proof is first constructed
    /// is subjective, some nodes will receive the necessary messages before others. Also, this number cannot be
    /// reconstructed for blocks finalized before extracting data from the node.
    ///
    /// Value will initially be null until the block containing the finalization proof for this block is itself finalized. 
    /// </summary>
    public double? FinalizationTime { get; set; }
}