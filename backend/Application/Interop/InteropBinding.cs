using System.Runtime.InteropServices;

namespace Application.Interop;

/// <summary>
/// Contains FFI bindings.
/// </summary>
internal static class InteropBinding
{
    private const string DllName = "librust_bindings";
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "schema_display")]
    private static extern bool schema_display(string schema, FFIOption schema_version, ref IntPtr result);
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_receive_contract_parameter")]
    private static extern bool get_receive_contract_parameter(string schema, FFIOption schema_version, string contract_name, string entrypoint, string value, ref IntPtr result);
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_event_contract")]
    private static extern bool get_event_contract(string schema, FFIOption schema_version, string contract_name, string value, ref IntPtr result);
    
    internal static InteropResult SchemaDisplay(string schema, FFIOption schemaVersion)
    {
        var result = IntPtr.Zero;
        try
        {
            var schemaDisplay = schema_display(schema, schemaVersion, ref result);
            var ptrToStringAnsi = Marshal.PtrToStringAnsi(result);
            return new InteropResult(ptrToStringAnsi, schemaDisplay);
        }
        finally
        {
            Free(result);            
        }
    }
    
    internal static InteropResult GetReceiveContractParameter(string schema, string contractName, string entrypoint, string value, FFIOption schemaVersion)
    {
        var result = IntPtr.Zero;
        try
        {
            var schemaDisplay = get_receive_contract_parameter(schema, schemaVersion, contractName, entrypoint, value, ref result);
            var ptrToStringAnsi = Marshal.PtrToStringAnsi(result);
            return new InteropResult(ptrToStringAnsi, schemaDisplay);
        }
        finally
        {
            Free(result);    
        }
    }
    
    internal static InteropResult GetEventContract(string schema, string contractName, string value, FFIOption schemaVersion)
    {
        var result = IntPtr.Zero;
        try
        {
            var schemaDisplay = get_event_contract(schema, schemaVersion, contractName, value, ref result);
            var ptrToStringAnsi = Marshal.PtrToStringAnsi(result);
            return new InteropResult(ptrToStringAnsi, schemaDisplay);
        }
        finally
        {
            Free(result);   
        }
    }
    
    internal readonly record struct InteropResult(string? Message, bool Succeeded);
    
    private static void Free(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct FFIOption
    {
        private ushort t;
        private bool is_some;

        public static FFIOption None() => new() { is_some = false };
        public static FFIOption Some(ushort some) => new()
        {
            t = some,
            is_some = true
        };
    }
}
