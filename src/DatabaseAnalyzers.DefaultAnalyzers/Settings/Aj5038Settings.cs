using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5038")]
internal sealed class Aj5038SettingsRaw : IRawDiagnosticSettings<Aj5038Settings>
{
    [Description("Defines the name quoting policy for object creations.")]
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyDuringObjectCreation { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;

    [Description("Defines the name quoting policy for column references.")]
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnReferences { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;

    [Description("Defines the name quoting policy for column definitions during creation.")]
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnDefinitions { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;

    [Description("Defines the name quoting policy for table references.")]
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForTableReferences { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;

    [Description("Defines the name quoting policy for data types.")]
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForDataTypes { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;

    public Aj5038Settings ToSettings() => new
    (
        NameQuotingPolicyDuringObjectCreation,
        NameQuotingPolicyForColumnReferences,
        NameQuotingPolicyForColumnDefinitions,
        NameQuotingPolicyForTableReferences,
        NameQuotingPolicyForDataTypes
    );
}

public sealed record Aj5038Settings(
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyDuringObjectCreation,
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnReferences,
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnDefinitions,
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForTableReferences,
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForDataTypes
) : IDiagnosticSettings<Aj5038Settings>
{
    public static Aj5038Settings Default { get; } = new
    (
        Aj5038SettingsNameQuotingPolicy.Undefined,
        Aj5038SettingsNameQuotingPolicy.Undefined,
        Aj5038SettingsNameQuotingPolicy.Undefined,
        Aj5038SettingsNameQuotingPolicy.Undefined,
        Aj5038SettingsNameQuotingPolicy.Undefined
    );

    public static string DiagnosticId => "AJ5038";
}
