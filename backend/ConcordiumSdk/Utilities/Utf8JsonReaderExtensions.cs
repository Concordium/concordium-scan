using System.Text.Json;

namespace ConcordiumSdk.Utilities;

public static class Utf8JsonReaderExtensions
{
    public static String? ReadString(this Utf8JsonReader readerClone, string propertyName, bool throwIfNotFound = true)
    {
        var propertyFound = ForwardReaderToPropertyValue(ref readerClone, propertyName, throwIfNotFound);
        return propertyFound ? readerClone.GetString() : null;
    }

    public static Int32? ReadInt32(this Utf8JsonReader readerClone, string propertyName, bool throwIfNotFound = true)
    {
        var propertyFound = ForwardReaderToPropertyValue(ref readerClone, propertyName, throwIfNotFound);
        return propertyFound ? readerClone.GetInt32() : null;
    }

    public static bool HasPropertyNamed(this Utf8JsonReader readerClone, string propertyName)
    {
        var propertyFound = ForwardReaderToPropertyValue(ref readerClone, propertyName, false);
        return propertyFound;
    }

    private static bool ForwardReaderToPropertyValue(ref Utf8JsonReader readerClone, string propertyName, bool throwIfNotFound) 
    {
        if (readerClone.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a start object");
        
        var startDepth = readerClone.CurrentDepth;
        readerClone.Read();

        var found = false;
        while (!(found = readerClone.TokenType == JsonTokenType.PropertyName
                         && readerClone.CurrentDepth == startDepth + 1
                         && readerClone.GetString() == propertyName)
               && !(readerClone.TokenType == JsonTokenType.EndObject
                    && readerClone.CurrentDepth == startDepth))
            readerClone.Read();

        if (!found)
        {
            if (throwIfNotFound)
                throw new JsonException($"Could not find property named '{propertyName}'");
            return false;
        }

        readerClone.Read();
        return true;
    }
}