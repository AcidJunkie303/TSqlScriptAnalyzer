using System.Collections.Immutable;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5015")]
internal sealed class Aj5015SettingsRaw : IRawDiagnosticSettings<Aj5015Settings>
{
    [Description("List of of suppressed missing index warnings. For 'FullColumnNamePattern', Wildcards like `*` and `?` are supported.")]
    public IReadOnlyList<Aj5015SettingsSettingsEntryRaw>? MissingIndexSuppressions { get; set; }

    [Description("Maximum size of an index in bytes. Please not that depending on the target SQL Server, the actual maximum size may be different. Default value is 1700 bytes.")]
    public int MaximumIndexSizeInBytes { get; set; }

    [Description("Data type names to ignore. Please do not include any size specifications. For example, use `NVARCHAR` instead of `NVARCHAR(100)`.")]
    public IReadOnlyList<string?>? IgnoredDataTypeNames { get; set; }

    public Aj5015Settings ToSettings()
    {
        var maximumIndexSizeInBytes = MaximumIndexSizeInBytes <= 0
            ? Aj5015Settings.DefaultMaximumIndexSizeInBytes
            : MaximumIndexSizeInBytes;

        return MissingIndexSuppressions is null
            ? Aj5015Settings.Default
            : new Aj5015Settings(
                maximumIndexSizeInBytes,
                MissingIndexSuppressions.Select(static a => a.ToSettings()).ToImmutableArray(),
                IgnoredDataTypeNames.EmptyIfNull().WhereNotNull().ToImmutableHashSet()
            );
    }
}

public sealed record Aj5015Settings(
    int MaximumIndexSizeInBytes,
    IReadOnlyList<Aj5015SettingsSettingsEntry> MissingIndexSuppressions,
    IReadOnlySet<string> IgnoredDataTypeNames
) : IDiagnosticSettings<Aj5015Settings>
{
    public static int DefaultMaximumIndexSizeInBytes => 1_700;

    public static Aj5015Settings Default { get; } = new Aj5015SettingsRaw
    {
        MaximumIndexSizeInBytes = DefaultMaximumIndexSizeInBytes,
        MissingIndexSuppressions = [new Aj5015SettingsSettingsEntryRaw { FullColumnNamePattern = "*.sys.*", SuppressionReason = "Built-in schema" }],
        IgnoredDataTypeNames = ["SQL_VARIANT", "XML", "GEOGRAPHY", "GEOMETRY", "HIERARCHYID"]
    }.ToSettings();

    public static string DiagnosticId => "AJ5015";
}
