using Concordium.Sdk.Types;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Configurations;

public sealed class TransactionTypeType : EnumType<TransactionType>
{
    protected override void Configure(IEnumTypeDescriptor<TransactionType> descriptor)
    {
        // Change enum names from gRPC v2 to align with gRPC v1 to avoid breaking schema changes.
        descriptor.Name("AccountTransactionType");

        // Change enum value names from those in gRPC v2 to avoid breaking schema changes.
        descriptor.Value(TransactionType.InitContract)
            .Name("INITIALIZE_SMART_CONTRACT_INSTANCE");
        descriptor.Value(TransactionType.Update)
            .Name("UPDATE_SMART_CONTRACT_INSTANCE");
        descriptor.Value(TransactionType.Transfer)
            .Name("SIMPLE_TRANSFER");
        descriptor.Value(TransactionType.EncryptedAmountTransfer)
            .Name("ENCRYPTED_TRANSFER");
        descriptor.Value(TransactionType.TransferWithMemo)
            .Name("SIMPLE_TRANSFER_WITH_MEMO");
        descriptor.Value(TransactionType.EncryptedAmountTransferWithMemo)
            .Name("ENCRYPTED_TRANSFER_WITH_MEMO");
        descriptor.Value(TransactionType.TransferWithScheduleAndMemo)
            .Name("TRANSFER_WITH_SCHEDULE_WITH_MEMO");
    }
}