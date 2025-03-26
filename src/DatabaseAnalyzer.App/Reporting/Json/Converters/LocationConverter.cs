using System.Text.Json;
using System.Text.Json.Serialization;
using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.App.Reporting.Json.Converters;

internal class LocationConverter : JsonConverter<CodeLocation>
{
    public override CodeLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, CodeLocation value, JsonSerializerOptions options) => writer.WriteRawValue($$"""{ "Line": {{value.Line}}, "Column": {{value.Column}} }""");
}
