using System.Text.Json;

namespace ConcordiumSdk.Utilities;

public static class Utf8JsonReaderExtensions
{
    public static bool ForwardReaderToPropertyValue(this ref Utf8JsonReader readerClone, string propertyName, bool throwIfNotFound = true)
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

    public static String? ReadString(this Utf8JsonReader readerClone, string propertyName, bool throwIfNotFound = true)
    {
        var propertyFound = readerClone.ForwardReaderToPropertyValue(propertyName, throwIfNotFound);
        return propertyFound ? readerClone.GetString() : null;
    }

    public static Int32? ReadInt32(this Utf8JsonReader readerClone, string propertyName, bool throwIfNotFound = true)
    {
        var propertyFound = readerClone.ForwardReaderToPropertyValue(propertyName, throwIfNotFound);
        return propertyFound ? readerClone.GetInt32() : null;
    }

    public static bool HasPropertyNamed(this Utf8JsonReader readerClone, string propertyName)
    {
        var propertyFound = readerClone.ForwardReaderToPropertyValue(propertyName, false);
        return propertyFound;
    }

    public static void ForwardReaderToTokenTypeAtDepth(this ref Utf8JsonReader reader, JsonTokenType tokenType, int depth)
    {
        var success = true;
        while (!(reader.TokenType == tokenType && reader.CurrentDepth == depth) && success)
            success = reader.Read();
        
        if (!success) 
            throw new InvalidOperationException($"Did not find token type '{tokenType}' at depth '{depth}' in this reader.");
    }

}