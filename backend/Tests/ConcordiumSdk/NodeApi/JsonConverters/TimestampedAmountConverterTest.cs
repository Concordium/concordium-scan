using System.Text.Json;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.NodeApi.Types.JsonConverters;
using ConcordiumSdk.Types;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class TimestampedAmountConverterTest
{
    [Fact]
    public void RoundTrip()
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new TimestampedAmountConverter() }
        };

        var json = "[1621260359123, \"1000000\"]";
        var deserialized = JsonSerializer.Deserialize<TimestampedAmount>(json, serializerOptions);
        Assert.NotNull(deserialized);
        Assert.Equal(new DateTimeOffset(2021, 05, 17, 14, 05, 59, 123, TimeSpan.Zero), deserialized!.Timestamp);
        Assert.Equal(CcdAmount.FromMicroCcd(1000000), deserialized.Amount);

        var serialized = JsonSerializer.Serialize(deserialized, serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}