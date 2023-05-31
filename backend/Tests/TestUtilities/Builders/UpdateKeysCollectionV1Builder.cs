using Concordium.Sdk.Types.New;

namespace Tests.TestUtilities.Builders;

public class UpdateKeysCollectionV1Builder
{
    public UpdateKeysCollectionV1 Build()
    {
        var anyAccessStructure = new AccessStructure(Array.Empty<ushort>(), 0);
        return new UpdateKeysCollectionV1(
            new HigherLevelAccessStructureRootKeys(Array.Empty<UpdatePublicKey>(), 0),
            new HigherLevelAccessStructureLevel1Keys(Array.Empty<UpdatePublicKey>(), 0),
            new AuthorizationsV1(Array.Empty<UpdatePublicKey>(), anyAccessStructure, anyAccessStructure,
                anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure,
                anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure,
                anyAccessStructure, anyAccessStructure, anyAccessStructure));
    }
}