using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class Aj5038SettingsRaw : IRawSettings<Aj5038Settings>
{
    public NameQuotingPolicy NameQuotingPolicyDuringObjectCreation { get; set; } = NameQuotingPolicy.Undefined;
    public NameQuotingPolicy NameQuotingPolicyForColumnReferences { get; set; } = NameQuotingPolicy.Undefined;
    public NameQuotingPolicy NameQuotingPolicyForTableAliases { get; set; } = NameQuotingPolicy.Undefined;
    public NameQuotingPolicy NameQuotingPolicyForSchemaNameReferences { get; set; } = NameQuotingPolicy.Undefined;

    public Aj5038Settings ToSettings() => new
    (
        NameQuotingPolicyDuringObjectCreation,
        NameQuotingPolicyForColumnReferences,
        NameQuotingPolicyForTableAliases,
        NameQuotingPolicyForSchemaNameReferences
    );
}

public sealed record Aj5038Settings(
    NameQuotingPolicy NameQuotingPolicyDuringObjectCreation,
    NameQuotingPolicy NameQuotingPolicyForColumnReferences,
    NameQuotingPolicy NameQuotingPolicyForTableAliases,
    NameQuotingPolicy NameQuotingPolicyForSchemaNameReferences
) : ISettings<Aj5038Settings>
{
    public static Aj5038Settings Default { get; } = new
    (
        NameQuotingPolicy.Undefined,
        NameQuotingPolicy.Undefined,
        NameQuotingPolicy.Undefined,
        NameQuotingPolicy.Undefined
    );

    public static string DiagnosticId => "AJ5039";
}
