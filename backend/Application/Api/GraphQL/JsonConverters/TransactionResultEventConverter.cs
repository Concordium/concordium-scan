namespace Application.Api.GraphQL.JsonConverters;

public class TransactionResultEventConverter : PolymorphicJsonConverter<TransactionResultEvent>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(Transferred), 1 },
        { typeof(AccountCreated), 2 },
        { typeof(CredentialDeployed), 3 },
    };

    public TransactionResultEventConverter() : base(SerializeMap)
    {
    }
}