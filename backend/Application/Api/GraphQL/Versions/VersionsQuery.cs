using System.Reflection;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Versions;

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