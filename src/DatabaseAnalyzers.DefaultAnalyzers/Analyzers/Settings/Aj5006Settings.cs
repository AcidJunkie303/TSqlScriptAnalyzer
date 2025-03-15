using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5006")]
internal sealed class Aj5006SettingsRaw : IRawSettings<Aj5006Settings>
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
            .EmptyIfNull()
            .WhereNotNull()
            .Select(static a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray();
}

public sealed record Aj5006Settings(
    [property: Description("Banned data types for columns. Wildcards like `*` and `?` are supported.")]
    IReadOnlyCollection<Regex> BannedColumnDataTypes,
    [property: Description("Banned data types function parameters. Wildcards like `*` and `?` are supported.")]
    IReadOnlyCollection<Regex> BannedFunctionParameterDataTypes,
    [property: Description("Banned data types for procedure parameters. Wildcards like `*` and `?` are supported.")]
    IReadOnlyCollection<Regex> BannedProcedureParameterDataTypes,
    [property: Description("Banned data types for variables. Wildcards like `*` and `?` are supported.")]
    IReadOnlyCollection<Regex> BannedScriptVariableDataTypes
) : ISettings<Aj5006Settings>
{
    public static Aj5006Settings Default { get; } = new([], [], [], []);
    public static string DiagnosticId => "AJ5006";
}
