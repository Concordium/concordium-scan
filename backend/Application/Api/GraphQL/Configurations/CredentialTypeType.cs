using Concordium.Sdk.Types;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Configurations;

public sealed class CredentialTypeType : EnumType<CredentialType>
{
    protected override void Configure(IEnumTypeDescriptor<CredentialType> descriptor)
    {
        // Change enum names from gRPC v2 to align with gRPC v1 to avoid breaking schema changes.
        descriptor.Name("CredentialDeploymentTransactionType");
    }
}