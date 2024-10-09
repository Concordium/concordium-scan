using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Import.Validations;

public interface IImportValidator
{
    Task Validate(Block block, ProtocolVersion protocolVersion);
}
