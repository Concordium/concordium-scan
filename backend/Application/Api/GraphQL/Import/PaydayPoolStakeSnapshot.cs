namespace Application.Api.GraphQL.Import;

public record PaydayPoolStakeSnapshot(
    PaydayPoolStakeSnapshotItem[] Items);
    
public record PaydayPoolStakeSnapshotItem(
    long BakerId,
    long BakerStake,
    long DelegatedStake);
