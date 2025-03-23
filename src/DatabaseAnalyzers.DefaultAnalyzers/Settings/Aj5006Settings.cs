using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5006")]
internal sealed class Aj5006SettingsRaw : IRawDiagnosticSettings<Aj5006Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    [Description("Banned data types for columns. Wildcards like `*` and `?` are supported.")]
    public IReadOnlyCollection<string?>? BannedColumnDataTypes { get; set; }

    [Description("Banned data types function parameters. Wildcards like `*` and `?` are supported.")]
    public IReadOnlyCollection<string?>? BannedFunctionParameterDataTypes { get; set; }

    [Description("Banned data types for procedure parameters. Wildcards like `*` and `?` are supported.")]
    public IReadOnlyCollection<string?>? BannedProcedureParameterDataTypes { get; set; }

    [Description("Banned data types for variables. Wildcards like `*` and `?` are supported.")]
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
    IReadOnlyCollection<Regex> BannedColumnDataTypes,
    IReadOnlyCollection<Regex> BannedFunctionParameterDataTypes,
    IReadOnlyCollection<Regex> BannedProcedureParameterDataTypes,
    IReadOnlyCollection<Regex> BannedScriptVariableDataTypes
) : IDiagnosticSettings<Aj5006Settings>
{
    public static Aj5006Settings Default { get; } = new([], [], [], []);
    public static string DiagnosticId => "AJ5006";
}
