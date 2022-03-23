using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class AccountAddressConverter : ValueConverter<AccountAddress, string>
{
    public AccountAddressConverter() : base(
        v => ConvertToString(v),
        v => ConvertToAccountAddress(v))
    {
    }
    private static AccountAddress ConvertToAccountAddress(string value)
    {
        return new AccountAddress(value);
    }

    private static string ConvertToString(AccountAddress value)
    {
        return value.AsString;
    }
    
}