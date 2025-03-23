using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

// ReSharper disable once CheckNamespace
namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5030")]
public sealed class Aj5030SettingsRaw : IRawDiagnosticSettings<Aj5030Settings>
{
    internal static Regex AlwaysMatchRegex { get; } = new(string.Empty, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)); // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization

    [Description("The naming policy for columns.")]
    public PatternEntryRaw? ColumnName { get; set; }

    [Description("The naming policy for functions.")]
    public PatternEntryRaw? FunctionName { get; set; }

    [Description("The naming policy for parameters.")]
    public PatternEntryRaw? ParameterName { get; set; }

    [Description("The naming policy for procedures.")]
    public PatternEntryRaw? ProcedureName { get; set; }

    [Description("The naming policy for tables.")]
    public PatternEntryRaw? TableName { get; set; }

    [Description("The naming policy for temp tables.")]
    public PatternEntryRaw? TempTableName { get; set; }

    [Description("The naming policy for triggers.")]
    public PatternEntryRaw? TriggerName { get; set; }

    [Description("The naming policy for variables.")]
    public PatternEntryRaw? VariableName { get; set; }

    [Description("The naming policy for views.")]
    public PatternEntryRaw? ViewName { get; set; }

    [Description("The naming policy for table aliases.")]
    public PatternEntryRaw? TableAliasName { get; set; }

    [Description("The object names to exclude from this rule. Wildcards like `*` and `?` are supported.")]
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
        TableAliasName: ToPatternEntry(TableAliasName),
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
    Aj5030Settings.PatternEntry ColumnName,
    Aj5030Settings.PatternEntry FunctionName,
    Aj5030Settings.PatternEntry ParameterName,
    Aj5030Settings.PatternEntry ProcedureName,
    Aj5030Settings.PatternEntry TableName,
    Aj5030Settings.PatternEntry TempTableName,
    Aj5030Settings.PatternEntry TriggerName,
    Aj5030Settings.PatternEntry VariableName,
    Aj5030Settings.PatternEntry ViewName,
    Aj5030Settings.PatternEntry TableAliasName,
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
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty),
        []
    );

    public static string DiagnosticId => "AJ5030";

    public sealed record PatternEntry(Regex Pattern, string Description);
}
