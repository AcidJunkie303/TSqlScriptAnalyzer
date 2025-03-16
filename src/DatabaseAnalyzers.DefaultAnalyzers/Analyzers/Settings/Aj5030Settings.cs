using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

// ReSharper disable once CheckNamespace
namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5030")]
public sealed class Aj5030SettingsRaw : IRawDiagnosticSettings<Aj5030Settings>
{
    internal static Regex AlwaysMatchRegex { get; } = new(string.Empty, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)); // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization

    public PatternEntryRaw? ColumnName { get; set; }
    public PatternEntryRaw? FunctionName { get; set; }
    public PatternEntryRaw? ParameterName { get; set; }
    public PatternEntryRaw? PrimaryKeyConstraintName { get; set; }
    public PatternEntryRaw? ProcedureName { get; set; }
    public PatternEntryRaw? TableName { get; set; }
    public PatternEntryRaw? TempTableName { get; set; }
    public PatternEntryRaw? TriggerName { get; set; }
    public PatternEntryRaw? VariableName { get; set; }
    public PatternEntryRaw? ViewName { get; set; }
    public IReadOnlyCollection<string?>? IgnoredObjectNamePatterns { get; set; }

    public Aj5030Settings ToSettings() => new
    (
        ColumnName: ToPatternEntry(ColumnName),
        FunctionName: ToPatternEntry(FunctionName),
        ParameterName: ToPatternEntry(ParameterName),
        ProcedureName: ToPatternEntry(ProcedureName),
        TableName: ToPatternEntry(TableName),
        TempTableName: ToPatternEntry(TempTableName),
        TriggerName: ToPatternEntry(TriggerName),
        VariableName: ToPatternEntry(VariableName),
        ViewName: ToPatternEntry(ViewName),
        IgnoredObjectNamePatterns: IgnoredObjectNamePatterns
                                       ?.WhereNotNullOrWhiteSpaceOnly()
                                       .Select(a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
                                       .ToImmutableList()
                                   ?? []
    );

    private static Regex ToRegex(string pattern) => new(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static Aj5030Settings.PatternEntry ToPatternEntry(PatternEntryRaw? patternEntryRaw)
        => (patternEntryRaw?.Pattern).IsNullOrWhiteSpace()
            ? new Aj5030Settings.PatternEntry(AlwaysMatchRegex, string.Empty)
            : new Aj5030Settings.PatternEntry(ToRegex(patternEntryRaw.Pattern), patternEntryRaw.Description ?? $" does not comply with the regular expression  {patternEntryRaw.Pattern}");

    public sealed class PatternEntryRaw
    {
        public string? Description { get; set; }
        public string? Pattern { get; set; }
    }
}

public sealed record Aj5030Settings(
    [property: Description("The naming policy for columns.")]
    Aj5030Settings.PatternEntry ColumnName,
    [property: Description("The naming policy for functions.")]
    Aj5030Settings.PatternEntry FunctionName,
    [property: Description("The naming policy for parameters.")]
    Aj5030Settings.PatternEntry ParameterName,
    [property: Description("The naming policy for procedures.")]
    Aj5030Settings.PatternEntry ProcedureName,
    [property: Description("The naming policy for tables.")]
    Aj5030Settings.PatternEntry TableName,
    [property: Description("The naming policy for temp tables.")]
    Aj5030Settings.PatternEntry TempTableName,
    [property: Description("The naming policy for triggers.")]
    Aj5030Settings.PatternEntry TriggerName,
    [property: Description("The naming policy for variables.")]
    Aj5030Settings.PatternEntry VariableName,
    [property: Description("The naming policy for views.")]
    Aj5030Settings.PatternEntry ViewName,
    [property: Description("The object names to exclude from this rule. Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Regex> IgnoredObjectNamePatterns)
    : IDiagnosticSettings<Aj5030Settings>
{
    public static Aj5030Settings Default { get; } = new
    (
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        []
    );

    public static string DiagnosticId => "AJ5030";

    public sealed record PatternEntry(Regex Pattern, string Description);
}
