using System.IO;
using System.IO.Compression;
using System.Text;
using Application.Common.Diagnostics;
using Application.Database;
using Dapper;
using Npgsql;

namespace Application.Import.ConcordiumNode;

public class NodeCacheRepository
{
    private readonly DatabaseSettings _dbSettings;
    private readonly IMetrics _metrics;

    public NodeCacheRepository(DatabaseSettings dbSettings, IMetrics metrics)
    {
        _dbSettings = dbSettings;
        _metrics = metrics;
    }
    
    public void WriteBlockSummary(string blockHash, string content)
    {
        using var counter = _metrics.MeasureDuration(nameof(NodeCache), nameof(WriteBlockSummary));

        using var conn = new NpgsqlConnection(_dbSettings.ConnectionStringNodeCache);
        conn.Open();

        var sql = @"insert into block_summary (block_hash, compressed_data) values (@BlockHash, @CompressedData)";
        var args = new
        {
            BlockHash = blockHash, 
            CompressedData = Compress(content)
        };
        
        conn.Execute(sql, args);
    }

    public string? ReadBlockSummary(string blockHash)
    {
        using var counter = _metrics.MeasureDuration(nameof(NodeCache), nameof(ReadBlockSummary));

        using var conn = new NpgsqlConnection(_dbSettings.ConnectionStringNodeCache);
        conn.Open();

        var sql = @"select compressed_data from block_summary where block_hash = @BlockHash;";
        var args = new { BlockHash = blockHash };
        
        var result = conn.ExecuteScalar<byte[]>(sql, args);
        return result != null ? Decompress(result) : null;
    }

    private static byte[] Compress(string text) 
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var outputStream = new MemoryStream();
        using var zipStream = new GZipStream(outputStream, CompressionMode.Compress);
        zipStream.Write(bytes, 0, bytes.Length);
        zipStream.Flush();
        return outputStream.ToArray();
    }

    private static string Decompress(byte[] compressed) 
    {
        using var inputStream = new MemoryStream(compressed);
        using var zipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        zipStream.CopyTo(outputStream);
        var decompressedBytes = outputStream.ToArray();
        return Encoding.UTF8.GetString(decompressedBytes);
    }
}