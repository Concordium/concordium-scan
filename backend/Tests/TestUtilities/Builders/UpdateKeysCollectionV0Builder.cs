using Concordium.Sdk.Types.New;

namespace Tests.TestUtilities.Builders;

public class UpdateKeysCollectionV0Builder
{
    public UpdateKeysCollectionV0 Build()
    {
        var anyAccessStructure = new AccessStructure(Array.Empty<ushort>(), 0);
        return new UpdateKeysCollectionV0(
            new HigherLevelAccessStructureRootKeys(Array.Empty<UpdatePublicKey>(), 0),
            new HigherLevelAccessStructureLevel1Keys(Array.Empty<UpdatePublicKey>(), 0),
            new AuthorizationsV0(Array.Empty<UpdatePublicKey>(), anyAccessStructure, anyAccessStructure,
                anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure,
                anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure));
    }
}