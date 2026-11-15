using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdvancedPCBuilder.Infrastructure;

/// <summary>JSON settings for the JSON columns: camelCase names and enums as strings (readable in the DB file).</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
