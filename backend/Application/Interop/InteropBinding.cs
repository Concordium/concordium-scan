using System.Runtime.InteropServices;
using Application.Aggregates.Contract.Types;
using Application.Exceptions;

// ReSharper disable InconsistentNaming
// Disabling are because names should follow the names in rust code. 

namespace Application.Interop;

/// <summary>
/// Contains FFI bindings which can be compiled to work on platforms Linux, OSX and Windows.
/// </summary>
internal static class InteropBinding
{
    private const string DllName = "librust_bindings";
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "schema_display")]
    private static extern bool schema_display(string schema, FFIByteOption schema_version, ref IntPtr result);
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_receive_contract_parameter")]
    private static extern bool get_receive_contract_parameter(string schema, FFIByteOption schema_version, string contract_name, string entrypoint, string value, ref IntPtr result);
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_event_contract")]
    private static extern bool get_event_contract(string schema, FFIByteOption schema_version, string contract_name, string value, ref IntPtr result);
    
    /// <summary>
    /// Get module schema in a human interpretable form.
    /// </summary>
    /// <param name="schema">Module schema in hexadecimal</param>
    /// <param name="schemaVersion">Optional schema version if present from module</param> 
    /// <returns>Module schema in a human interpretable form</returns>
    internal static string? SchemaDisplay(string schema, ModuleSchemaVersion? schemaVersion)
    {
        var ffiOption = ModuleSchemaVersionExtensions.Into(schemaVersion);
        var result = IntPtr.Zero;
        try
        {
            var schemaDisplay = schema_display(schema, ffiOption, ref result);
            var resultStringAnsi = Marshal.PtrToStringAnsi(result);
            
            if (!schemaDisplay)
            {
                throw InteropBindingException.Create(resultStringAnsi);
            }
            
            return resultStringAnsi;
        }
        finally
        {
            FreeIfNonzero(result);            
        }
    }
    
    /// <summary>
    /// Get contract receive parameters in a human interpretable form.
    ///
    /// Receive parameters are those given to a contract entrypoint on a update call.
    /// </summary>
    /// <param name="schema">Module schema in hexadecimal</param>
    /// <param name="contractName">Contract name</param>
    /// <param name="entrypoint">Entrypoint of contract</param>
    /// <param name="value">Receive parameters in hexadecimal</param>
    /// <param name="schemaVersion">Optional schema version if present from module</param> 
    /// <returns>Receive parameters in a human interpretable form</returns>
    internal static string? GetReceiveContractParameter(string schema, string contractName, string entrypoint, string value, ModuleSchemaVersion? schemaVersion)
    {
        var ffiOption = ModuleSchemaVersionExtensions.Into(schemaVersion);
        var result = IntPtr.Zero;
        try
        {
            var schemaDisplay = get_receive_contract_parameter(schema, ffiOption, contractName, entrypoint, value, ref result);
            var resultStringAnsi = Marshal.PtrToStringAnsi(result);
            
            if (!schemaDisplay)
            {
                throw InteropBindingException.Create(resultStringAnsi);
            }
            
            return resultStringAnsi;
        }
        finally
        {
            FreeIfNonzero(result);    
        }
    }
    
    /// <summary>
    /// Get contract event in a human interpretable form.
    /// </summary>
    /// <param name="schema">Module schema in hexadecimal</param>
    /// <param name="contractName">Contract name</param>
    /// <param name="value">Contract event in hexadecimal</param>
    /// <param name="schemaVersion">Optional schema version if present from module</param>
    /// <returns>Contract event in a human interpretable form</returns>
    internal static string? GetEventContract(string schema, string contractName, string value, ModuleSchemaVersion? schemaVersion)
    {
        var ffiOption = ModuleSchemaVersionExtensions.Into(schemaVersion);
        var result = IntPtr.Zero;
        try
        {
            var schemaDisplay = get_event_contract(schema, ffiOption, contractName, value, ref result);
            var resultStringAnsi = Marshal.PtrToStringAnsi(result);
            
            if (!schemaDisplay)
            {
                throw InteropBindingException.Create(resultStringAnsi);
            }
            
            return resultStringAnsi;
        }
        finally
        {
            FreeIfNonzero(result);   
        }
    }
    
    private static void FreeIfNonzero(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
    
    /// <summary>
    /// A C# layout which compiled to a C interpretable structure. This is used as an optional parameter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FFIByteOption
    {
        internal byte t { get; private init; }
        /// <summary>
        /// 1 is interpreted as true. <see cref="Boolean"/> are not used since it isn't a blittable type.
        /// </summary>
        /// <remarks>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types">Blittable and Non-Blittable Types</see>
        /// </remarks>
        internal byte is_some { get; private init; }

        public static FFIByteOption None() => new() { is_some = 0 };
        public static FFIByteOption Some(byte some) => new()
        {
            t = some,
            is_some = 1
        };
    }
}
