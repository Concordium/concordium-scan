using System.IO;
using System.Text.Json;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;
using FluentAssertions;
using Moq;
using Tests.TestUtilities.Stubs;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractAddress = Application.Api.GraphQL.ContractAddress;
using ContractInitialized = Application.Api.GraphQL.Transactions.ContractInitialized;
using ContractVersion = Application.Api.GraphQL.ContractVersion;

namespace Tests.Api.GraphQL.Transactions;

public class TransactionResultEventTests
{
    private readonly JsonSerializerOptions _serializerOptions = EfCoreJsonSerializerOptionsFactory.Create();

    private async Task<ModuleReferenceEvent> CreateModuleReferenceEventWithCis2WccdSchema()
    {
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        
        return new ModuleReferenceEvent(
            0,
            "",
            0,
            0,
            "foo",
            new AccountAddress(""),
            "",
            schema,
            ModuleSchemaVersion.Undefined,
            ImportSource.NodeImport,
            DateTimeOffset.UtcNow
        );
    }
    [Fact]
    public async Task GivenContractUpdated_WhenParseMessageAndEvents_ThenParsed()
    {
        // Arrange
        const string contractName = "cis2_wCCD";
        const string entrypoint = "wrap";
        const string message = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";
        const string eventMessage = "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";
        const string expectedMessage = "{\"data\":\"\",\"to\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]}}";
        const string expectedEvent = "{\"Mint\":{\"amount\":\"1000000\",\"owner\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]},\"token_id\":\"\"}}";

        var moduleReferenceEvent = await CreateModuleReferenceEventWithCis2WccdSchema();
        
        var contractUpdated = new ContractUpdated(
            new ContractAddress(1,0),
            new AccountAddress(""),
            42,
            message,
            $"{contractName}.{entrypoint}",
            ContractVersion.V0,
            new []{eventMessage});

        var moduleRepositoryMock = new Mock<IModuleReadonlyRepository>();
        moduleRepositoryMock.Setup(m => m.GetModuleReferenceEventAtAsync(It.IsAny<ContractAddress>(), It.IsAny<ulong>(),
            It.IsAny<ulong>(), It.IsAny<uint>()))
            .Returns(Task.FromResult(moduleReferenceEvent));

        // Act
        var updated = await contractUpdated.TryUpdate(moduleRepositoryMock.Object, 0, 0, 0);
        
        // Assert
        updated.Should().NotBeNull();
        updated!.Message.Should().Be(expectedMessage);
        updated.Events![0].Should().Be(expectedEvent);
    }
    
        [Fact]
    public async Task GivenContractInitialized_WhenParseEvents_ThenParsed()
    {
        // Arrange
        const string contractName = "cis2_wCCD";
        const string eventMessage = "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";
        const string expectedEvent = "{\"Mint\":{\"amount\":\"1000000\",\"owner\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]},\"token_id\":\"\"}}";
        
        var moduleReferenceEvent = await CreateModuleReferenceEventWithCis2WccdSchema();

        var contractInitialized = new ContractInitialized(
            "",
            new ContractAddress(1, 0),
            10,
            $"init_{contractName}",
            ContractVersion.V0,
            new[]{eventMessage});
        
        var moduleRepositoryMock = new Mock<IModuleReadonlyRepository>();
        moduleRepositoryMock.Setup(m => m.GetModuleReferenceEventAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(moduleReferenceEvent));

        // Act
        var updated = await contractInitialized.TryUpdateWithParsedEvents(moduleRepositoryMock.Object);
        
        // Assert
        updated.Should().NotBeNull();
        updated!.Events![0].Should().Be(expectedEvent);
    }
    
    [Fact]
    public async Task GivenContractInterrupted_WhenParseEvents_ThenParsed()
    {
        // Arrange
        const string contractName = "cis2_wCCD";
        const string eventMessage = "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";
        const string expectedEvent = "{\"Mint\":{\"amount\":\"1000000\",\"owner\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]},\"token_id\":\"\"}}";
        
        var moduleReferenceEvent = await CreateModuleReferenceEventWithCis2WccdSchema();

        var contractInitialized = new ContractInitialized(
            "",
            new ContractAddress(1, 0),
            10,
            $"init_{contractName}",
            ContractVersion.V0,
            Array.Empty<string>());
        var contractInterrupted = new ContractInterrupted(
            new ContractAddress(1, 0),
            new[]{eventMessage});
        
        var moduleRepositoryMock = new Mock<IModuleReadonlyRepository>();
        moduleRepositoryMock.Setup(m => m.GetModuleReferenceEventAtAsync(It.IsAny<ContractAddress>(), It.IsAny<ulong>(),
                It.IsAny<ulong>(), It.IsAny<uint>()))
            .Returns(() => Task.FromResult(moduleReferenceEvent));
        var contractRepositoryMock = new Mock<IContractRepository>();
        contractRepositoryMock.Setup(m => m.GetReadonlyContractInitializedEventAsync(It.IsAny<ContractAddress>()))
            .Returns(Task.FromResult(contractInitialized));

        // Act
        var updated = await contractInterrupted.TryUpdateWithParsedEvents(
            contractRepositoryMock.Object,
            moduleRepositoryMock.Object,
            0,
            0,
            0);
        
        // Assert
        updated.Should().NotBeNull();
        updated!.Events![0].Should().Be(expectedEvent);
    }
    
    [Fact]
    public void GivenInvokedContract_WhenDeserializeAndSerialize_ThenParse()
    {
        // Arrange
        var updated = TransactionResultEventStubs.ContractUpdated();
        var instigator = new ContractAddress(1,0);
        var invoked = new ContractAddress(2, 0);
        updated = updated with
        {
            ContractAddress = invoked,
            Instigator = instigator
        };
        TransactionResultEvent invokedContract = new ContractCall(updated);
        
        // Act
        var serialized = JsonSerializer.Serialize(invokedContract, _serializerOptions);
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(serialized, _serializerOptions);
        
        // Assert
        deserialized.Should().BeOfType<ContractCall>();
        var actualInvoked = (deserialized as ContractCall)!;
        actualInvoked.ContractUpdated.ContractAddress.Index.Should().Be(invoked.Index);
        actualInvoked.ContractUpdated.Instigator.Should().BeOfType<ContractAddress>();
        using var document = JsonDocument.Parse(serialized);
        document.RootElement.TryGetProperty("data", out var data).Should().BeTrue();
        data.TryGetProperty("ContractUpdated", out var contractUpdated).Should().BeTrue();
        contractUpdated.TryGetProperty("ContractAddress", out var contractAddress).Should().BeTrue();
        contractAddress.GetString().Should().Be("2,0");
    }
    
    [Fact]
    public void WhenDeserializeNullVersion_ThenReturnObjectWithNull()
    {
        // Arrange
        const string serialized = "{\"tag\":18,\"data\":{\"ContractAddress\":\"2544,0\",\"Instigator\":{\"tag\":1,\"data\":\"4cxrwJB2tBFW7nthCb3AACXnx5aQNwA1jL6uKWVNoV3yLuH5jY\"},\"Amount\":0,\"MessageAsHex\":\"64000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00ac7f861b8988bbe2567e051a9a9a1258d3456d8c0dee06b312c2b31638d761d500000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00ff4fc5840520d7091e977965b45ebc8c4bb3656f73cf71a679179d3631af718400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a006038645e83ff182931701b0d6004f634d726c6d77f1d57b3ae2bcd9adc18fa3400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00d43e0cba57f328306477c7936ec124a302193d1c3453969dbfc9bb239728e57a00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00d41e99b961c9a2c2c4ffac905592e124421cb7d8f667cd346ca659b20f1e4f0400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a004d75cb911325f1a7a214bf6384e20771921dd4b6db9d0823773412c091b50c0400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a008be1a223598a162f2e00f52d2df038abd5228ad1b1294e6e4b06d54b304c5a9d00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00e9084174906e5c55f588b3151d378906d6bf05d154b79ec6457d815bc79eedc200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00b3c61515e7043d624f6ac4f89d04e9df9ad13e5f6fcb08ab54e489640de8c38c00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0009278220d0eb1caa2bd5d7b6961cd889befccf146e9e24d799b5af30c7ce44ee00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a009e317f913527e105a0f7792cd932ff6d14ff23f6683c77fb5e058bb4d30cada100000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00976ed4d0d4e1cdb393b017b4f9dc736adf3b1c94562b32b3a681168d82f3488e00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0089fa0664dd3402a05342c857689d9005b84efe22e0b9ea5601c7528afdf760dd00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a008bde63ce9f30d0c89f779bea894d30e488d7b42a44e159ca19f574f436d4914700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00fcdaf5a658b9ffbb14f64e73e9b5841cc7c61b675204e5d9a1987e1dbce6594d00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00d11aa3b2c7e3ae5009176ca395bfef326f1c6ec9dc0d65e12cd996665d59e80200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a005ec1fae6558bb8e31ce2557ae687a4ceb8fe380c0bb73b6700b4d63b94ab0a4d00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00ad0788abdd268f839b6f4e2ddb270accbd9ff05455c633349bf1b7bc8a2f6f8000000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0013c43540f7e0422a541520c1ccccb85425545ab3052483cb1287f1e998454da500000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00f124a3e25d9cdc57462111d2db9bdfb0b4cb90891217501fc85fcb856bf362bf00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0079c80129690530a26e1fac53c6858d2d289a5ed8e014cd4bc518c0d6474d73d200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a002d4c3d5963c51acb5b0cec12383f39aafeefbe0280ce35279ce40cc09643e78700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00c8a63df4c28e2d93be04393ef1c5b56443576b7f23381a965f25d3edcca636c200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00dc181503b53e7f2faddf390bb2cafa1b946544a4fc293b1ce9ad49188e67645700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00c1dba88c83916f5c9c51e9579626825fc34b5b4faae7e8664518f7d0189ba62200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00c5517307ac973c0b75e0f58c5302d769c8f5131781b6b921a5eb081ea15034ee00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00f5fe0c424cc23ac990d83b8e0915ee4e08accadfd48327f1a2709d747dafc7ff00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00a26bfffa6310569c62a3156e63df7b093a47b391f195e0c38871e0c714f43a1200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00d5f950e1a6f6fc9090053cb8ef5f87b530ddae0e5b121a5cd41d2dadee747be100000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00a3133787f4addeadb4662ccb743052d4cca0724602df69f5580690ed0c94139900000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00619bb3dec3c7fe9d1d9ba6d3b75e972133158992262b82fae26297a01f12d7b500000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0076990b7f1f70e19fb4b51af8531e40f535f05d28878d9c33b9457a6e2b5adc5d00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a005373b52994ceb9ecb15eda786aba75f0e6b56b5ca3906a09e37a59202cc6cfd700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00278d08ed7428d91dfea3c7920f4a19fda932ab8bed7ee67720887d29532ed47800000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00ee5f42937e3676f18dcc7e29216a35022521dc4504d5491e954a90670b5c53a900000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a007a69e161153f55745c56fc8a42190d65782b0c18752078e5bcf37a79e2aa916900000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0036006d06cf81c17122bbd0c75b91c41df48ee6109cca3887356fba46a5d9f19700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00783957af1eb8e33f57ffc57e890284d2a9dab9c0623cb864e01605242fa44f8a00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00d4ec2b4d9e12b62dbfda080f7caa9ac1407936495bbcb204f48f3fea7efc388800000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00792ad05204a08d02f90aa6f8cc95b12d267402283f886651bef72227cf0c911d00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a001ca04a041385a745b14939911cfa39754fffa52c7978fe9935d2a35df0b4dad200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a000f130f3cf028bd6bb310f8bd2cfa532fd569b90c6d3fd6f33af900483752cf1e00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00c8300672cf499190b035cc60c2edb59273a6a6e824fcfba81f75ef010af33cc400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00620e2a345f1e7b9e853ddcf8e7c9746e5cbfae9510dd3bc4ecdc3f0810d9f7f000000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a002cef2105c4668bebe50c29688c718b8106b63be6a54d04500a7555463f96db0400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00c3a685c2fb5c968a72d607563cfa1b6876bac3c95ef1b6d41ddfae48b6cf1adc00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a001558569ca10bafc04c341671200ccc89c22a4152d37aaf9292eb38cccd2a4eec00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a005fad23092c096e3e5b6e4694a7789f52cc7c365ede815a6665463fb96f80fdfa00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0001c64e1300118ca3bec1bc474a048b6e65503143f234271a2081ec9b5b5a1ac000000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00f173c6faa868e50433ed4d838efed145112663a17cb537a1c67e2caab08ae35700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a004837c2ea08af79eea67139d17ae489ac48bb64638b400a30db760207c386732200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00e385a7766eb1ce70c930a540bcbd0f86404a674a78094039cd72786f121190b400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a009af792edd80240234627b3c59fd3d85e20a80bf3ae1649d39996b492354bc74300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00e3bea9743481675db60d586812b96784122bd0a26e364b14d4e1e36e878f59e100000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a007b2118f70105e170588e6a70612790836f0aa866224ddedf7cedf5760799680300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0072eeb46c3ffc19ba2890aa1032c996e3192e4a8cd8a7b1a5df4871156e97c83e00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0052544fe34bc2a13df796bdba071374cb425c720b0db83616a9d9ed916424ffd300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a002cac224cefcd51560afffda41e82dd5bed32951a5552463db525907737fb685800000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00936eb1c0ddb31141f9f0f073250217a3467bf5def9187260da629e2872ac1a8300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00071cff39aed07937f914b1f1fdce2124280dc71f57335ae6ef67a4644d07a95300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00d531c6877f3a17f49428da9c8090f0f840f3211e91c540f26db7edf1ce4c180000000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a003d96b246c553fc50e5e23d7e8910c76dae8debe9e0695eb060d3e30e0a2e2feb00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00f54a780b6b1b27b171c2df6cf450c71581200bebeb361bd463517827d3eb381a00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0019dc45ec29ddab9600b20f2f57cee9e2097114522750fdc630ca2d13bf0e82df00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00daaa86826c454051c3384cce9cacc45111c74ebe73ac723314751eae0da3424600000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a006479c009aa2c504e0d658c32697f7d45b0b55d6c6457cfcdf5956bea1b27e7ee00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00436f7c7cdb7ac7a3faf72ff623e93ff62bfcd236d91b6c99ba29492acedac7ef00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0080813051a42a393acea092bf7f06e2f6f98203e58e2452ca5fccbef43bab43f400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0014a5b4bd944e56855ea096c9fb2246d7668077bd70e096fdc718d04c724ac0aa00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00c5d5b3c64a397b995375dea4393b4f14eb94b51241c95f88e5c0320c3b48d05100000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00b456384369b4f9e96fb8eda422ed2b9890e72f1ca7d1640ea22dd519846c6e7100000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00a6ba3b9299b1452cdaf8716f8a9cf10ab40f62361b18d1818d228d7bab37884d00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a009f7bce28d7f8e93d5b64518e966d8dd8919414a27ae03d00870206b4de179e8b00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a001645199dfa596bb32d82652d68cdc204dc6c0d1b7ace24e5f5d59b762956029200000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a001a5dbfb3b7750275c0728b2409b82ede71b7bfed544c37b683dabd6b47982f5600000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00aaba75937982cb68aea907766671b492ca53dbe3c8ef24949d584dcfdad7bf0300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0064050243eaa8125e9870c1255bec7aa031ff028b05f1b47e2e038f074440a8e500000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00844c46ad25c910e46287798380e19de9754b1b6d236cdcad809f871dc02fb23400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00cf0c94468c2b073df37fdd23336601b7e3e74b1b86424cf1353623f5bfe3bdb600000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00d2d566bbc6f570b163a375185341e45981871baef51d481ec7b6918a843e24df00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00783cf1a74a6bf0a73f0a863b8d6759d8dc7e0b85e0f6567ba66117ef78dfa63900000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0090968c91dd837c314a097631b6ba55b184c1c4185c512ee670b9cca8abcb55d900000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a008559c3b8434c22fca89f35c57fac8ccb2186fdbe324cf6f504732854681a829700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00bcb3f858eb7e56006030fee7aed86b03ba0fda99b23294814e3c9d4c94d7989000000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a008d5ef159bf778670d5b889ca836ff56f308953eb8612b48f05268002b669b75300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00ad6591eabd47212d8578b2f3328dcbcf546290b00c57c9c58c95e977249cfaa300000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00ba278e88ce2cd99fbdd60ce1f2369fe347c244481978dca19887d441c8076e3e00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0005f894159ec2c9027b72972e12e8d56a012ed11b03e7c233abcce67f6b2f942d00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a008ad76f898c9a124af5af7aeb02993f38eccad9ad6e07657965091daddceb377700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a003cced1a4ebc6e1fa64462d61fba917c219e0fcb786fff8fd112922c81359ea8e00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00e9f9b41b2653c6ba40d23b52efb09753addb16f17b61ec93a69682889b194b2a00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a002c9eaf068cc22fab3e92d5f54714997c4ef0ad0fe93575c60652886a09aeb4a900000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a001bc72263ab6e4cb00798c8f8b5f15476a0be434caf20023593dd8d1e9d6f172700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00a5f18e2e099d86eda8918ae1c94f12b7da441b2f3a94447767c22abe480191fe00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00f9be7b7ee744034875d4eee332b32d4d84701669980c93bbedfbc9e9ecd8633c00000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0011e76a0f93b36add1045485600ff3b9504ce36e629f171f6d9906daf91fdbbc400000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a001a3dc07e40b1e663ba129a2bc17f2770d79dcca62fc5ffd9954d186d3bb5d57800000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a0010236763ebbea18bfe70dacaf320805597d7c771313b02558986d9d232dea6c100000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a008861decb534e632a6c6862310e32cb72f8816b934d26a19b1a9e98fb9d0b55d700000080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00dafaad318b53ee5860159377103c3e82b53888053e1be839a8c74d9aa89591f00000\",\"ReceiveName\":\"cis2_OVL.transfer\",\"EventsAsHex\":[\"ff0080d0dbc3f40200dcbf3818670601587afa68b2eb6cd16a153a8d2456749ae0d6157e69fce2041a00ac7f861b8988bbe2567e051a9a9a1258d3456d8c0dee06b312c2b31638d761d5\"]}}";
        
        // Act
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(serialized, _serializerOptions);
        
        // Assert
        deserialized.Should().BeOfType<ContractUpdated>();
        var contractUpdated = deserialized as ContractUpdated;
        contractUpdated!.Version.Should().BeNull();
    }

    [Fact]
    public void WhenSerializeNullVersion_ThenInsertNull()
    {
        // Arrange
        var updated = TransactionResultEventStubs.ContractUpdated();
        
        // Act
        var serialized = JsonSerializer.Serialize(updated, _serializerOptions);
        
        // Act
        using var document = JsonDocument.Parse(serialized);
        var tryGetProperty = document.RootElement.TryGetProperty("Version", out var version);
        tryGetProperty.Should().BeTrue();
        version.ValueKind.Should().Be(JsonValueKind.Null);
    }
}

