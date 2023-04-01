using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Modules
{
    public record ContractModuleSchema
    {

        [ID]
        public long Id { get; set; }
        public string ModuleRef { get; set; }
        public string SchemaName { get; set; }
        public string SchemaHex { get; set; }

        public ContractModuleSchema(string moduleRef, string schemaName, string schemaHex)
        {
            ModuleRef = moduleRef;
            SchemaName = schemaName;
            SchemaHex = schemaHex;
        }
    }
}
