namespace Application.Api.GraphQL.JsonConverters;

public class TransactionResultEventConverter : PolymorphicJsonConverter<TransactionResultEvent>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(Transferred), 1 },
        { typeof(AccountCreated), 2 },
        { typeof(CredentialDeployed), 3 },
        { typeof(BakerAdded), 4 },
        { typeof(BakerKeysUpdated), 5 },
        { typeof(BakerRemoved), 6 },
        { typeof(BakerSetRestakeEarnings), 7 },
        { typeof(BakerStakeDecreased), 8 },
        { typeof(BakerStakeIncreased), 9 },
    };

    public TransactionResultEventConverter() : base(SerializeMap)
    {
    }
}