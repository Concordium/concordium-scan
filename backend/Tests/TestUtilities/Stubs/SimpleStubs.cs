using System.Collections.Generic;
using System.Collections.Immutable;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Stubs;

internal class SimpleStubs
{
    internal static ImmutableHashSet<UpdateKeysIndex> AuthorizedKeysStub() => ImmutableHashSet<UpdateKeysIndex>.Empty;

    internal static AccessStructure AccessStructureStub() => new AccessStructure(
        AuthorizedKeysStub(),
        new UpdateKeysThreshold(1)
    );

    internal static RootKeys RootKeysStub() => new RootKeys(new List<UpdatePublicKey>(), new UpdateKeysThreshold(1));
    
    internal static Level1Keys Level1KeysStub() => new Level1Keys(new List<UpdatePublicKey>(), new UpdateKeysThreshold(1));

    internal static AuthorizationsV0 AuthorizationsV0Stub() =>
        new(
            new List<UpdatePublicKey>(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub(),
            AccessStructureStub()
        );

    internal static AuthorizationsV1 AuthorizationsV1Stub() =>
        new(
            AuthorizationsV0Stub(),
            AccessStructureStub(),
            AccessStructureStub()
        );
}