using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5062")]
internal sealed class Aj5062SettingsRaw : IRawDiagnosticSettings<Aj5062Settings>
{
    public bool ValueRequiredForNullableParameters { get; set; }
    public bool ValueRequiredForParametersWithDefaultValue { get; set; }

    public Aj5062Settings ToSettings() => new
    (
        ValueRequiredForNullableParameters,
        ValueRequiredForParametersWithDefaultValue
    );
}

public sealed record Aj5062Settings(
    [property: Description("A value must be specified for nullable parameters.")]
    bool ValueRequiredForNullableParameters,
    [property: Description("A value must be specified for paramters with a default value.")]
    bool ValueRequiredForParametersWithDefaultValue
) : IDiagnosticSettings<Aj5062Settings>
{
    public static Aj5062Settings Default { get; } = new(true, true);

    public static string DiagnosticId => "AJ5062";
}
