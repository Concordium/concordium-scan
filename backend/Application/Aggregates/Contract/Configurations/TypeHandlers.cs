using System.Data;
using System.Text.Json;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Transactions;
using Dapper;

namespace Application.Aggregates.Contract.Configurations;

public class TransactionRejectReasonHandler : SqlMapper.TypeHandler<TransactionRejectReason>
{
    public override void SetValue(IDbDataParameter parameter, TransactionRejectReason value)
    {
        throw new NotImplementedException();
    }

    public override TransactionRejectReason Parse(object value)
    {
        return JsonSerializer.Deserialize<TransactionRejectReason>(value.ToString()!, EfCoreJsonSerializerOptionsFactory.Default)!;
    }
}

public class TransactionResultEventHandler : SqlMapper.TypeHandler<TransactionResultEvent>
{
    public override void SetValue(IDbDataParameter parameter, TransactionResultEvent value)
    {
        throw new NotImplementedException();
    }

    public override TransactionResultEvent Parse(object value)
    {
        return JsonSerializer.Deserialize<TransactionResultEvent>(value.ToString()!, EfCoreJsonSerializerOptionsFactory.Default)!;
    }
}
    
public class TransactionTypeUnionHandler : SqlMapper.TypeHandler<TransactionTypeUnion>
{
    public override void SetValue(IDbDataParameter parameter, TransactionTypeUnion value)
    {
        throw new NotImplementedException();
    }

    public override TransactionTypeUnion Parse(object value)
    {
        return TransactionTypeToStringConverter.ConvertToTransactionTypeUnion(value.ToString()!);
    }
}
    
public class AccountAddressHandler : SqlMapper.TypeHandler<AccountAddress?>
{
    public override void SetValue(IDbDataParameter parameter, AccountAddress? value)
    {
        throw new NotImplementedException();
    }

    public override AccountAddress? Parse(object? value)
    {
        return value is null ? null : new AccountAddress(value.ToString()!);
    }
}
