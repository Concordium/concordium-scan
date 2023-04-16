using System.Runtime.InteropServices;

namespace Application.Api.GraphQL.Import.Modules
{
    public static partial class Interop
    {
        public const string NativeLib = "libconcordium_scan_native.so";

        static Interop()
        {
        }

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "deserialize_recieve_message")]
        public static extern IntPtr deserialize_recieve_message(string return_value_bytes, string module_schema, string contract_name, string function_name, Optionu8 schema_version);
    }

    ///Option type containing boolean flag and maybe valid data.
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Optionu8
    {
        ///Element that is maybe valid.
        byte t;
        ///Byte where `1` means element `t` is valid.
        byte is_some;
    }

    public partial struct Optionu8
    {
        public static Optionu8 FromNullable(byte? nullable)
        {
            var result = new Optionu8();
            if (nullable.HasValue)
            {
                result.is_some = 1;
                result.t = nullable.Value;
            }

            return result;
        }

        public byte? ToNullable()
        {
            return this.is_some == 1 ? this.t : (byte?)null;
        }
    }

    public class InteropException<T> : Exception
    {
        public T Error { get; private set; }

        public InteropException(T error) : base($"Something went wrong: {error}")
        {
            Error = error;
        }
    }
}
