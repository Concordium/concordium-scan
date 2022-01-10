using System.Text.Json;

namespace Tests.TestUtilities;

public static class JsonAssert
{
    public static void Equivalent(string expected, string actual)
    {
        var expectedJson = JsonDocument.Parse(expected).RootElement;
        var actualJson = JsonDocument.Parse(actual).RootElement;
        var comparer = new JsonElementComparer();
        
        var equal = comparer.Equals(expectedJson, actualJson);
        Assert.True(equal);
    }
    
}