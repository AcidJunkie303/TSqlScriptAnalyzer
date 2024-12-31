using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

// ReSharper disable once CheckNamespace
namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
public sealed class Aj5030SettingsRaw : IRawSettings<Aj5030Settings>
{
    internal static Regex AlwaysMatchRegex { get; } = new(string.Empty, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)); // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public string? ColumnNamePattern { get; set; }
    public string? FunctionNamePattern { get; set; }
    public string? ParameterNamePattern { get; set; }
    public string? PrimaryKeyConstraintNamePattern { get; set; }
    public string? ProcedureNamePattern { get; set; }
    public string? TableNamePattern { get; set; }
    public string? TriggerNamePattern { get; set; }
    public string? VariableNamePattern { get; set; }
    public string? ViewNamePattern { get; set; }

    public Aj5030Settings ToSettings() => new
    (
        ToRegex(ColumnNamePattern),
        ToRegex(FunctionNamePattern),
        ToRegex(ParameterNamePattern),
        ToRegex(PrimaryKeyConstraintNamePattern),
        ToRegex(ProcedureNamePattern),
        ToRegex(TableNamePattern),
        ToRegex(TriggerNamePattern),
        ToRegex(VariableNamePattern),
        ToRegex(ViewNamePattern)
    );

    private static Regex ToRegex(string? pattern)
        => pattern.IsNullOrWhiteSpace()
            ? AlwaysMatchRegex
            : new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
}

public sealed record Aj5030Settings(
    Regex ColumnNamePattern,
    Regex FunctionNamePattern,
    Regex ParameterNamePattern,
    Regex PrimaryKeyConstraintNamePattern,
    Regex ProcedureNamePattern,
    Regex TableNamePattern,
    Regex TriggerNamePattern,
    Regex VariableNamePattern,
    Regex ViewNamePattern
) : ISettings<Aj5030Settings>
{
    public static Aj5030Settings Default { get; } = new
    (
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex,
        Aj5030SettingsRaw.AlwaysMatchRegex
    );

    public static string DiagnosticId => "AJ5030";
}
