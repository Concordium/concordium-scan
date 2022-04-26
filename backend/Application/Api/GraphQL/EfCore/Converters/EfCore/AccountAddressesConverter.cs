using Application.Api.GraphQL.Accounts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class AccountAddressesConverter : ValueConverter<AccountAddress[], string[]>
{
    public AccountAddressesConverter() : base(
        v => ConvertToString(v),
        v => ConvertToAccountAddress(v))
    {
    }
    private static AccountAddress[] ConvertToAccountAddress(string[] value)
    {
        return value.Select(x => new AccountAddress(x)).ToArray();
    }

    private static string[] ConvertToString(AccountAddress[] value)
    {
        return value.Select(x => x.AsString).ToArray();
    }
}