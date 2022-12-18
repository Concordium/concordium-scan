using System.Reflection;
using Application.Api.GraphQL;
using HotChocolate.Types;

[ExtendObjectType(typeof(Query))]
public class VersionsQuery
{
    public Versions GetVersions()
    {
        return new Versions()
        {
            BackendVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0"
        };
    }
}