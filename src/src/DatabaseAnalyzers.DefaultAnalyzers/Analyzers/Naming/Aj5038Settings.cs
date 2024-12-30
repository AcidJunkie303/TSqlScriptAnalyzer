using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class Aj5038SettingsRaw : IRawSettings<Aj5038Settings>
{
    public NameQuotingPolicy NameQuotingPolicyDuringObjectCreation { get; set; } = NameQuotingPolicy.Undefined;
    public NameQuotingPolicy NameQuotingPolicyForColumnReferences { get; set; } = NameQuotingPolicy.Undefined;
    public NameQuotingPolicy NameQuotingPolicyForColumnDefinitions { get; set; } = NameQuotingPolicy.Undefined;
    public NameQuotingPolicy NameQuotingPolicyForTableReferences { get; set; } = NameQuotingPolicy.Undefined;
    public NameQuotingPolicy NameQuotingPolicyForDataTypes { get; set; } = NameQuotingPolicy.Undefined;

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
    NameQuotingPolicy NameQuotingPolicyDuringObjectCreation,
    NameQuotingPolicy NameQuotingPolicyForColumnReferences,
    NameQuotingPolicy NameQuotingPolicyForColumnDefinitions,
    NameQuotingPolicy NameQuotingPolicyForTableReferences,
    NameQuotingPolicy NameQuotingPolicyForDataTypes
) : ISettings<Aj5038Settings>
{
    public static Aj5038Settings Default { get; } = new
    (
        NameQuotingPolicy.Undefined,
        NameQuotingPolicy.Undefined,
        NameQuotingPolicy.Undefined,
        NameQuotingPolicy.Undefined,
        NameQuotingPolicy.Undefined
    );

    public static string DiagnosticId => "AJ5039";
}
