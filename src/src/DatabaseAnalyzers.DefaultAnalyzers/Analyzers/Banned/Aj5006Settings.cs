using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
public sealed class Aj5006SettingsRaw : IRawSettings<Aj5006Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyCollection<string?>? BannedColumnDataTypes { get; set; }
    public IReadOnlyCollection<string?>? BannedFunctionParameterDataTypes { get; set; }
    public IReadOnlyCollection<string?>? BannedProcedureParameterDataTypes { get; set; }
    public IReadOnlyCollection<string?>? BannedScriptVariableDataTypes { get; set; }

    public Aj5006Settings ToSettings() => new
    (
        ConvertToRegex(BannedColumnDataTypes),
        ConvertToRegex(BannedFunctionParameterDataTypes),
        ConvertToRegex(BannedProcedureParameterDataTypes),
        ConvertToRegex(BannedScriptVariableDataTypes)
    );

    private static ImmutableArray<Regex> ConvertToRegex(IReadOnlyCollection<string?>? types)
        => types
            .NullIfEmpty()
            .WhereNotNull()
            .Select(static a => a.ToRegexWithSimpleWildcards(compileRegex: true))
            .ToImmutableArray();
}

public sealed record Aj5006Settings(
    IReadOnlyCollection<Regex> BannedColumnDataTypes,
    IReadOnlyCollection<Regex> BannedFunctionParameterDataTypes,
    IReadOnlyCollection<Regex> BannedProcedureParameterDataTypes,
    IReadOnlyCollection<Regex> BannedScriptVariableDataTypes
) : ISettings<Aj5006Settings>
{
    public static Aj5006Settings Default { get; } = new([], [], [], []);
    public static string DiagnosticId => "AJ5006";
}
