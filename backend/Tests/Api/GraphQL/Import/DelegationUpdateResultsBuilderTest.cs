using Application.Api.GraphQL;
using Application.Api.GraphQL.Import;
using FluentAssertions;

namespace Tests.Api.GraphQL.Import;

public class DelegationUpdateResultsBuilderTest
{
    private readonly DelegationImportHandler.DelegationUpdateResultsBuilder _target;

    public DelegationUpdateResultsBuilderTest()
    {
        _target = new DelegationImportHandler.DelegationUpdateResultsBuilder();
    }

    [Fact]
    public void Build_NoDelegationTargetsChanged()
    {
        var result = _target.Build();
        result.DelegatorCountDeltas.Should().BeEmpty();
    }
    
    [Fact]
    public void Build_DelegationTargetsChanged()
    {
        _target.DelegationTargetRemoved(new BakerDelegationTarget(1));
        _target.DelegationTargetAdded(new BakerDelegationTarget(2));
        _target.DelegationTargetAdded(new BakerDelegationTarget(2));
        _target.DelegationTargetAdded(new BakerDelegationTarget(2));
        _target.DelegationTargetRemoved(new BakerDelegationTarget(2));
        _target.DelegationTargetAdded(new PassiveDelegationTarget());
        
        var result = _target.Build();
        result.DelegatorCountDeltas.Length.Should().Be(3);
        result.DelegatorCountDeltas.Should().Contain(new DelegationTargetDelegatorCountDelta(new BakerDelegationTarget(1), -1));
        result.DelegatorCountDeltas.Should().Contain(new DelegationTargetDelegatorCountDelta(new BakerDelegationTarget(2), 2));
        result.DelegatorCountDeltas.Should().Contain(new DelegationTargetDelegatorCountDelta(new PassiveDelegationTarget(), 1));
    }
    
    [Fact]
    public void Build_DelegationTargetsChanged_RemovesZeroCountItems()
    {
        _target.DelegationTargetAdded(new BakerDelegationTarget(1));
        _target.DelegationTargetAdded(new PassiveDelegationTarget());
        _target.DelegationTargetRemoved(new PassiveDelegationTarget());
        
        var result = _target.Build();
        result.DelegatorCountDeltas.Length.Should().Be(1);
        result.DelegatorCountDeltas.Should().Contain(new DelegationTargetDelegatorCountDelta(new BakerDelegationTarget(1), 1));
    }
}