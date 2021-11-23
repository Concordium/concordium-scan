using System;

namespace Application.Import.ConcordiumNode.GrpcClient;

public readonly struct BlockHash
{
    private readonly string _formatted;
    private readonly byte[] _value;

    public BlockHash(byte[] value)
    {
        if (value.Length != 32) throw new ArgumentException("value must be 32 bytes");
        _value = value;
        _formatted = Convert.ToHexString(value);
    }

    public BlockHash(string value)
    {
        if (value.Length != 64) throw new ArgumentException("string must be 64 char hex string.");
        _value = Convert.FromHexString(value);
        _formatted = value;
    }

    public string AsString => _formatted;
    public byte[] AsBytes => _value;
    
    public override string ToString()
    {
        return _formatted;
    }
}