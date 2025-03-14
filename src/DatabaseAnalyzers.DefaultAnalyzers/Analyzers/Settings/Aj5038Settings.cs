using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5038SettingsRaw : IRawSettings<Aj5038Settings>
{
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyDuringObjectCreation { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnReferences { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnDefinitions { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;
    public Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForTableReferences { get; set; } = Aj5038SettingsNameQuotingPolicy.Undefined;
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

internal sealed record Aj5038Settings(
    [property: Description("Defines the name quoting policy for object creations.")]
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyDuringObjectCreation,
    [property: Description("Defines the name quoting policy for column references.")]
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnReferences,
    [property: Description("Defines the name quoting policy for column definitions during creation.")]
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForColumnDefinitions,
    [property: Description("Defines the name quoting policy for table references.")]
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForTableReferences,
    [property: Description("Defines the name quoting policy for data types.")]
    Aj5038SettingsNameQuotingPolicy NameQuotingPolicyForDataTypes
) : ISettings<Aj5038Settings>
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
