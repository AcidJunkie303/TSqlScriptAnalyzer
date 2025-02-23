using System.Collections.Immutable;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Model;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5052SettingsRaw : IRawSettings<Aj5052Settings>
{
    public const string IndexPropertiesList = "PrimaryKey, Clustered, NonClustered, Unique, ColumnStore, Hash, Filtered, FullText, Spatial, Xml, Bitmap, Covering, WithIncludedColumns, ComputedColumns";

    public KeyValuePair<IndexProperties?, string>[]? NamingPatternByIndexProperties { get; set; }
    public string? DefaultPattern { get; set; }

    public Aj5052Settings ToSettings()
    {
        if (NamingPatternByIndexProperties is null)
        {
            return Aj5052Settings.Default;
        }

        var items = NamingPatternByIndexProperties
            .Where(a => a.Key.HasValue)
            .Where(a => !a.Value.IsNullOrWhiteSpace())
            .Select(a => KeyValuePair.Create(a.Key!.Value, a.Value))
            .ToImmutableArray();

        var defaultPattern = DefaultPattern.NullIfEmptyOrWhiteSpace()?.Trim() ?? Aj5052Settings.Default.DefaultPattern;
        return new Aj5052Settings(items, defaultPattern);
    }
}

internal sealed record Aj5052Settings(
    [property: Description($"Prioritized list where the key represents matching index properties and the value represents the pattern. Values for the key are: {Aj5052SettingsRaw.IndexPropertiesList}. To specify multiple index properties (key), separate them by a comma. The value supports the following placeholders: {Aj5052Settings.Placeholders.PlaceholdersList}")]
    IReadOnlyList<KeyValuePair<IndexProperties, string>> NamingPatternByIndexProperties,
    [property: Description($"In case there was no match, this pattern will be used. The following placeholders are supported: {Aj5052Settings.Placeholders.PlaceholdersList}")]
    string DefaultPattern
) : ISettings<Aj5052Settings>
{
    public static Aj5052Settings Default { get; } = new
    (
        NamingPatternByIndexProperties: new[]
        {
            KeyValuePair.Create(IndexProperties.PrimaryKey, $"PK_{Placeholders.TableSchemaName}_{Placeholders.TableName}"),
            KeyValuePair.Create(IndexProperties.Unique, $"UX_{Placeholders.TableSchemaName}_{Placeholders.TableName}")
        }.ToImmutableArray(),
        DefaultPattern: $"IX_{Placeholders.TableSchemaName}_{Placeholders.TableName}_{Placeholders.ColumnNames}"
    );

    public static string DiagnosticId => "AJ5052";

    internal static class Placeholders
    {
        public const string TableName = "{TableName}";
        public const string TableSchemaName = "{TableSchemaName}";
        public const string DatabaseName = "{DatabaseName}";
        public const string ColumnNames = "{ColumnNames}";

        public const string PlaceholdersList = $"{{{TableName}}}, {{{TableSchemaName}}}, {{{DatabaseName}}}, {{{ColumnNames}}}";
    }
}
