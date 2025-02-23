using System.ComponentModel;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5052SettingsRaw : IRawSettings<Aj5052Settings>
{
    public string? PrimaryKeyNamingPattern { get; set; }
    public string? UniqueIndexNamingPattern { get; set; }
    public string? ClusteredIndexNamingPattern { get; set; }
    public string? IndexNamingPattern { get; set; }

    public Aj5052Settings ToSettings() => new
    (
        PrimaryKeyNamingPattern: PrimaryKeyNamingPattern.IsNullOrWhiteSpace() ? Aj5052Settings.Default.PrimaryKeyNamingPattern : PrimaryKeyNamingPattern,
        UniqueIndexNamingPattern: UniqueIndexNamingPattern.IsNullOrWhiteSpace() ? Aj5052Settings.Default.UniqueIndexNamingPattern : UniqueIndexNamingPattern,
        ClusteredIndexNamingPattern: ClusteredIndexNamingPattern.IsNullOrWhiteSpace() ? Aj5052Settings.Default.ClusteredIndexNamingPattern : ClusteredIndexNamingPattern,
        IndexNamingPattern: IndexNamingPattern.IsNullOrWhiteSpace() ? Aj5052Settings.Default.IndexNamingPattern : IndexNamingPattern
    );
}

internal sealed record Aj5052Settings(
    [property: Description($"Naming pattern for primary keys. {Aj5052Settings.SupportedPlaceholdersMessageSuffix} Default Value: {Aj5052Settings.DefaultValues.PrimaryKeyNamingPattern}")]
    string? PrimaryKeyNamingPattern,
    [property: Description($"Naming pattern for unique indices. {Aj5052Settings.SupportedPlaceholdersMessageSuffix} Default Value: {Aj5052Settings.DefaultValues.UniqueIndexNamingPattern}")]
    string? UniqueIndexNamingPattern,
    [property: Description($"Naming pattern for clusterd indices. {Aj5052Settings.SupportedPlaceholdersMessageSuffix} Default Value: {Aj5052Settings.DefaultValues.ClusteredIndexNamingPattern}")]
    string? ClusteredIndexNamingPattern,
    [property: Description($"Naming pattern for other kind of indices. {Aj5052Settings.SupportedPlaceholdersMessageSuffix} Default Value: {Aj5052Settings.DefaultValues.IndexNamingPattern}")]
    string? IndexNamingPattern
) : ISettings<Aj5052Settings>
{
    internal const string SupportedPlaceholdersMessageSuffix = $"This is a case-sensitive regular expression pattern. The following placeholders are supported: {Placeholders.DatabaseName}, {Placeholders.TableSchemaName}, {Placeholders.TableName}, {Placeholders.ColumnNames}.";

    public static Aj5052Settings Default { get; } = new
    (
        PrimaryKeyNamingPattern: DefaultValues.PrimaryKeyNamingPattern,
        UniqueIndexNamingPattern: DefaultValues.UniqueIndexNamingPattern,
        ClusteredIndexNamingPattern: DefaultValues.ClusteredIndexNamingPattern,
        IndexNamingPattern: DefaultValues.IndexNamingPattern
    );

    public static string DiagnosticId => "AJ5052";

    internal static class DefaultValues
    {
        public const string PrimaryKeyNamingPattern = $"PK_{Placeholders.TableSchemaName}_{Placeholders.TableName}.*";
        public const string UniqueIndexNamingPattern = $"UX_{Placeholders.TableSchemaName}_{Placeholders.TableName}_{Placeholders.ColumnNames}.*";
        public const string ClusteredIndexNamingPattern = $"CX_{Placeholders.TableSchemaName}_{Placeholders.TableName}_{Placeholders.ColumnNames}.*";
        public const string IndexNamingPattern = $"IX_{Placeholders.TableSchemaName}_{Placeholders.TableName}_{Placeholders.ColumnNames}.*";
    }

    internal static class Placeholders
    {
        public const string TableName = "{TableName}";
        public const string TableSchemaName = "{TableSchemaName}";
        public const string DatabaseName = "{DatabaseName}";
        public const string ColumnNames = "{ColumnNames}";
    }
}
