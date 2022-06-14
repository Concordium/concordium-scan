namespace Application.Api.GraphQL.Bakers;

public class CurrentPaydayStatus
{
    public ulong BakerStake { get; set; }
    public ulong DelegatedStake { get; set; }
    public ulong EffectiveStake { get; set; }
    public decimal LotteryPower { get; set; }
}