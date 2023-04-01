using System.IO;
using WebAssembly;

namespace Application.Import.ConcordiumNode.Types.Modules
{
    internal class ModuleSource
    {
        private const int WASM_PREFIX_SIZE = 12;

        public string ModuleRef { get; }
        private Module Module { get; }

        public ModuleSource(string moduleRef, byte[] moduleSourceBytes)
        {
            ModuleRef = moduleRef;
            Module = Module.ReadFromBinary(new MemoryStream(moduleSourceBytes[WASM_PREFIX_SIZE..]));
        }

        public ModuleSchema? GetSchema()
        {
            var schemas = this.Module.CustomSections
                .Where(s => s.Name.StartsWith("concordium-schema"));

            if (!schemas.Any())
                return null;

            // Can a single module have multiple schema sections?
            var schema = schemas.First();
            return new ModuleSchema(
                ModuleRef,
                schema.Name,
                Convert.ToHexString(schema.Content.ToArray()));
        }
    }
}
