using HotChocolate.Types.Relay;

namespace Application.Import.ConcordiumNode.Types.Modules
{
    public record ModuleSchema
    {
        [ID]
        public long Id { get; set; }
        public string SchemaName { get; set; }
        public string ModuleRef { get; set; }
        public string SchemaHex { get; set; }

        public ModuleSchema(string moduleRef, string schemaName, string schemaHex)
        {
            ModuleRef = moduleRef;
            SchemaHex = schemaHex;
            SchemaName = schemaName;
        }
    }
}
