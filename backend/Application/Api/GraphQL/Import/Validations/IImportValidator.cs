using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;

namespace Application.Api.GraphQL.Import.Validations;

public interface IImportValidator
{
    Task Validate(Block block);
}