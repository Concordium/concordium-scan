namespace Application.Api.GraphQL;

public class AnonymityRevoker
{
    public AnonymityRevoker(int arIdentity, string name, string url, string description)
    {
        ArIdentity = arIdentity;
        Name = name;
        Url = url;
        Description = description;
    }

    public int ArIdentity { get; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Description { get; set; }
}