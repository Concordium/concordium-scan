using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class UpdateKeysCollectionBuilder
{
    public UpdateKeysCollection Build()
    {
        var anyAccessStructure = new AccessStructure(Array.Empty<ushort>(), 0);
        return new UpdateKeysCollection(
            new HigherLevelAccessStructureRootKeys(Array.Empty<UpdatePublicKey>(), 0),
            new HigherLevelAccessStructureLevel1Keys(Array.Empty<UpdatePublicKey>(), 0),
            new Authorizations(Array.Empty<UpdatePublicKey>(), anyAccessStructure, anyAccessStructure,
                anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure,
                anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure, anyAccessStructure));
    }
}