namespace Application.Api.GraphQL.Accounts;

public class Delegation
{
    /// <summary>
    /// EF-core constructor!
    /// </summary>
    private Delegation() {}

    public Delegation(bool restakeEarnings)
    {
        RestakeEarnings = restakeEarnings;
    }

    public bool RestakeEarnings { get; set; }
}