using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// Disabling are because names should follow the names in rust code. 

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
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "test_option")]
    private static extern IntPtr test_option(FFIOption schema_version);

    internal static string TestOption(FFIOption option)
    {
        var pointerWith = test_option(option);
        var stringWith = Marshal.PtrToStringAnsi(pointerWith);
        Marshal.FreeHGlobal(pointerWith);
        return stringWith!;
    }
    
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
    
    [StructLayout(LayoutKind.Sequential)]
    public struct FFIOption
    {
        internal byte t { get; private init; }
        internal byte is_some { get; private init; }

        public static FFIOption None() => new() { is_some = 0 };
        public static FFIOption Some(byte some) => new()
        {
            t = some,
            is_some = 1
        };
    }
}
