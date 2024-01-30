using System.Collections.Generic;
using Application.Import;
using Concordium.Sdk.Types;
using Moq;

namespace Tests.TestUtilities.Builders;

public class BlockDataPayloadBuilder
{
    private BlockInfo _blockInfo = new BlockInfoBuilder().Build();
    private IList<BlockItemSummary> _blockItemSummaries = new List<BlockItemSummary>();
    private readonly IChainParameters _chainParameters = Mock.Of<IChainParameters>();
    private readonly IList<ISpecialEvent> _specialEvents = new List<ISpecialEvent>();
    private AccountInfosRetrieved _accountInfos = new(Array.Empty<AccountInfo>(), Array.Empty<AccountInfo>());
    private RewardOverviewBase _rewardStatus = new RewardOverviewV0Builder().Build();
    private Func<Task<BakerPoolStatus[]>> _readAllBakerPoolStatusesFunc = () => Task.FromResult(Array.Empty<BakerPoolStatus>());
    private Func<Task<PassiveDelegationStatus>> _passiveDelegationPoolStatusFunc = () => Task.FromResult<PassiveDelegationStatus>(null!);

    public BlockDataPayload Build() =>
        new(
            _blockInfo,
            _blockItemSummaries,
            _chainParameters,
            _specialEvents,
            _accountInfos,
            _rewardStatus,
            _readAllBakerPoolStatusesFunc,
            _passiveDelegationPoolStatusFunc
        );

    public BlockDataPayloadBuilder WithPassiveDelegationPoolStatusFunc(Func<Task<PassiveDelegationStatus>> func)
    {
        _passiveDelegationPoolStatusFunc = func;
        return this;
    }

    public BlockDataPayloadBuilder WithAllBakerStatusesFunc(Func<Task<BakerPoolStatus[]>> func)
    {
        _readAllBakerPoolStatusesFunc = func;
        return this;
    }
    
    public BlockDataPayloadBuilder WithRewardStatus(RewardOverviewBase status)
    {
        _rewardStatus = status;
        return this;
    }
    
    public BlockDataPayloadBuilder WithAccountInfosRetrieved(AccountInfosRetrieved retrieved)
    {
        _accountInfos = retrieved;
        return this;
    }
    
    public BlockDataPayloadBuilder WithBlockItemSummaries(IList<BlockItemSummary> blockItemSummaries)
    {
        _blockItemSummaries = blockItemSummaries;
        return this;
    }
    
    public BlockDataPayloadBuilder WithBlockInfo(BlockInfo blockInfo)
    {
        _blockInfo = blockInfo;
        return this;
    }
}
