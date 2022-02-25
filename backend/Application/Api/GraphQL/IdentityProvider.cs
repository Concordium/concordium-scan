namespace Application.Api.GraphQL;

public class IdentityProvider
{
    public IdentityProvider(int ipIdentity, string name, string url, string description)
    {
        IpIdentity = ipIdentity;
        Name = name;
        Url = url;
        Description = description;
    }

    public int IpIdentity { get; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Description { get; set; }
}
