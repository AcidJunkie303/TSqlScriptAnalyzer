using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

// ReSharper disable once CheckNamespace
namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
public sealed class Aj5030SettingsRaw : IRawSettings<Aj5030Settings>
{
    internal static Regex AlwaysMatchRegex { get; } = new(string.Empty, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)); // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization

    public PatternEntryRaw? ColumnName { get; set; }
    public PatternEntryRaw? FunctionName { get; set; }
    public PatternEntryRaw? ParameterName { get; set; }
    public PatternEntryRaw? PrimaryKeyConstraintName { get; set; }
    public PatternEntryRaw? ProcedureName { get; set; }
    public PatternEntryRaw? TableName { get; set; }
    public PatternEntryRaw? TriggerName { get; set; }
    public PatternEntryRaw? VariableName { get; set; }
    public PatternEntryRaw? ViewName { get; set; }

    public Aj5030Settings ToSettings() => new
    (
        ToPatternEntry(ColumnName),
        ToPatternEntry(FunctionName),
        ToPatternEntry(ParameterName),
        ToPatternEntry(PrimaryKeyConstraintName),
        ToPatternEntry(ProcedureName),
        ToPatternEntry(TableName),
        ToPatternEntry(TriggerName),
        ToPatternEntry(VariableName),
        ToPatternEntry(ViewName)
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
    Aj5030Settings.PatternEntry PrimaryKeyConstraintName,
    Aj5030Settings.PatternEntry ProcedureName,
    Aj5030Settings.PatternEntry TableName,
    Aj5030Settings.PatternEntry TriggerName,
    Aj5030Settings.PatternEntry VariableName,
    Aj5030Settings.PatternEntry ViewName
) : ISettings<Aj5030Settings>
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
        new PatternEntry(Aj5030SettingsRaw.AlwaysMatchRegex, string.Empty)
    );

    public static string DiagnosticId => "AJ5030";

    public sealed record PatternEntry(Regex Pattern, string Description);
}
