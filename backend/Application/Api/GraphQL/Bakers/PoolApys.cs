namespace Application.Api.GraphQL.Bakers;

public class PoolApys
{
    public long PoolId { get; set; }
    public PoolApy Apy7Days { get; set; }
    public PoolApy Apy30Days { get; set; }
}

public class PoolApy
{
    private PoolApy()
    {
    }

    public PoolApy(double? totalApy, double? bakerApy, double? delegatorsApy)
    {
        TotalApy = totalApy;
        BakerApy = bakerApy;
        DelegatorsApy = delegatorsApy;
    }

    public double? TotalApy { get; set; }
    public double? BakerApy { get; set; }
    public double? DelegatorsApy { get; set; }
}