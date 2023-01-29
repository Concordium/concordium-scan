using Application.Api.GraphQL.Accounts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class ContractAddressConverter : ValueConverter<ContractAddress, string>
{
    public ContractAddressConverter() : base(
        v => ConvertToString(v),
        v => ConvertToContractAddress(v))
    {
    }
    private static ContractAddress ConvertToContractAddress(string value)
    {
        return new ContractAddress(value);
    }

    private static string ConvertToString(ContractAddress value)
    {
        return value.AsString;
    }
}