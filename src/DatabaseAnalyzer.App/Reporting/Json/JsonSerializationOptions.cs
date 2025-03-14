using System.Text.Encodings.Web;
using System.Text.Json;

namespace DatabaseAnalyzer.App.Reporting.Json;

internal static class JsonSerializationOptions
{
    public static JsonSerializerOptions Default { get; } =
        new(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
}
