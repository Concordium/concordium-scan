using System.IO;
using System.Runtime.InteropServices;
using Application.Api.GraphQL.Modules;
using Newtonsoft.Json;

namespace Application.Api.GraphQL.Import.Modules
{
    /// <summary>
    ///  Used to deserialize Method Parameters, Return Values & Events from Smart Contract executions on chain.
    /// </summary>
    public interface ISmartContractModuleSerDe
    {
        string? DeserializeReceiveMessage(string messageAsHex, string receiveName, ContractModuleSchema moduleSchema, int? schemaVersion = null);
    }

    /// <summary>
    ///  This class is used to deserialize Method Parameters, Return Values & Events from Smart Contract executions on chain.
    /// </summary>
    public class SmartContractModuleSerDe : ISmartContractModuleSerDe
    {
        private JsonSerializer jsonSerializer;

        public SmartContractModuleSerDe(JsonSerializer jsonSerializer)
        {
            this.jsonSerializer = jsonSerializer;
        }

        /// <summary>
        ///  Deserializes a receive message from a smart contract.
        /// </summary>
        /// <param name="messageAsHex">Hex encoded message</param>
        /// <param name="receiveName">Smart Contract Method Recieve Name</param>
        /// <param name="moduleSchema">Smart Contract module schema</param>
        /// <param name="schemaVersion">Smart Contract schema version</param>
        /// <returns></returns>
        public string? DeserializeReceiveMessage(
            string messageAsHex,
            string receiveName,
            ContractModuleSchema moduleSchema,
            int? schemaVersion = null)
        {
            var (contractName, methodName) = parsedReceiveName(receiveName);
            var res = this.DeserializeReceiveMessageValueInternal(messageAsHex, moduleSchema.SchemaHex, contractName, methodName, schemaVersion);

            if (String.IsNullOrWhiteSpace(res))
            {
                return null;
            }

            try
            {
                jsonSerializer.Deserialize(new JsonTextReader(new StringReader(res)));
                return res;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private string? DeserializeReceiveMessageValueInternal(string messageAsHex, string schemaHex, string contractName, string methodName, int? schemaVersion)
        {
            var intPtr = Interop.deserialize_recieve_message(messageAsHex, schemaHex, contractName, methodName, Optionu8.FromNullable((byte?)schemaVersion));
            var ret = Marshal.PtrToStringAnsi(intPtr);
            Marshal.FreeHGlobal(intPtr);

            return ret;
        }

        private (string contractName, string methodName) parsedReceiveName(string receiveName)
        {
            var parts = receiveName.Split(".");
            if (parts.Length != 2 || String.IsNullOrWhiteSpace(parts[0]) || String.IsNullOrWhiteSpace(parts[1]))
            {
                throw new ArgumentException(nameof(receiveName));
            }

            return (parts[0], parts[1]);
        }
    }
}
