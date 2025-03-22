using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class NameQuotingAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5038Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public NameQuotingAnalyzer(IScriptAnalysisContext context, Aj5038Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        AnalyzeObjectCreations(_settings.NameQuotingPolicyDuringObjectCreation, nameof(Aj5038SettingsRaw.NameQuotingPolicyDuringObjectCreation));
        AnalyzeColumnReferences(_settings.NameQuotingPolicyForColumnReferences, nameof(Aj5038SettingsRaw.NameQuotingPolicyForColumnReferences));
        AnalyzeColumnDefinitions(_settings.NameQuotingPolicyForColumnDefinitions, nameof(Aj5038SettingsRaw.NameQuotingPolicyForColumnDefinitions));
        AnalyzeTableReferences(_settings.NameQuotingPolicyForTableReferences, nameof(Aj5038SettingsRaw.NameQuotingPolicyForTableReferences));
        AnalyzeDataTypeReferences(_settings.NameQuotingPolicyForDataTypes, nameof(Aj5038SettingsRaw.NameQuotingPolicyForDataTypes));

        // TODO: Identifiers in object creation (lots of types to check)
        // TODO: Schema name references. Kinda hard...
    }

    private void AnalyzeDataTypeReferences(Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            _script.ParsedScript.GetChildren<SqlDataTypeReference>(recursive: true),
            static a => a.Name.Identifiers,
            "data type",
            policy,
            configurationKeyName);
    }

    private void AnalyzeTableReferences(Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        var tableReferences = _script.ParsedScript
            .GetChildren<NamedTableReference>(recursive: true)
            .Where(a =>
            {
                if (a.SchemaObject?.BaseIdentifier?.Value is null)
                {
                    return true;
                }

                return !a.SchemaObject.BaseIdentifier.Value.StartsWith('#');
            });

        Analyze(
            tableReferences,
            static a => a.SchemaObject.Identifiers.TakeLast(1),
            "table reference",
            policy,
            configurationKeyName);
    }

    private void AnalyzeColumnDefinitions(Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            _script.ParsedScript.GetChildren<ColumnDefinition>(recursive: true),
            static a => [a.ColumnIdentifier],
            "column definition",
            policy,
            configurationKeyName);
    }

    private void AnalyzeColumnReferences(Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            _script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true),
            static a => a.MultiPartIdentifier.Identifiers.TakeLast(1),
            "column",
            policy,
            configurationKeyName);
    }

    private void AnalyzeObjectCreations(Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            _script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true),
            static a => a.SchemaObjectName.Identifiers,
            "table",
            policy,
            configurationKeyName);

        Analyze(
            _script.ParsedScript.GetChildren<ViewStatementBody>(recursive: true),
            static a => a.SchemaObjectName.Identifiers,
            "view",
            policy,
            configurationKeyName);

        Analyze(
            _script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true),
            static a => a.ProcedureReference.Name.Identifiers,
            "procedure",
            policy,
            configurationKeyName);

        Analyze(
            _script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true),
            static a => a.Name.Identifiers,
            "function",
            policy,
            configurationKeyName);

        Analyze(
            _script.ParsedScript.GetChildren<TriggerStatementBody>(recursive: true),
            static a => a.Name.Identifiers,
            "trigger",
            policy,
            configurationKeyName);
    }

    private void Analyze<T>(IEnumerable<T> statements, Func<T, IEnumerable<Identifier>> identifierGetter, string typeName, Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string configurationKeyName)
        where T : TSqlFragment
    {
        foreach (var statement in statements)
        {
            Analyze(statement, identifierGetter, typeName, aj5038SettingsNameQuotingPolicy, configurationKeyName);
        }
    }

    private void Analyze<T>(T statement, Func<T, IEnumerable<Identifier>> identifierGetter, string typeName, Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string configurationKeyName)
        where T : TSqlFragment
    {
        foreach (var identifier in identifierGetter(statement))
        {
            if (identifier.Value.IsTempTableName())
            {
                continue;
            }

            var shouldBeWrittenAs = GetPolicyCompliantIdentifier(identifier, aj5038SettingsNameQuotingPolicy);
            if (shouldBeWrittenAs.IsNullOrWhiteSpace())
            {
                continue;
            }

            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
            var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
            _context.IssueReporter.Report(DiagnosticDefinitions.Default,
                databaseName,
                _script.RelativeScriptFilePath,
                fullObjectName,
                identifier.GetCodeRegion(),
                typeName, identifier.GetSql(), shouldBeWrittenAs, configurationKeyName);
        }
    }

    private static string? GetPolicyCompliantIdentifier(Identifier identifier, Aj5038SettingsNameQuotingPolicy quotingPolicy)
    {
        if (identifier.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        return quotingPolicy switch
        {
            Aj5038SettingsNameQuotingPolicy.Undefined              => null,
            Aj5038SettingsNameQuotingPolicy.Required               => identifier.QuoteType is QuoteType.DoubleQuote or QuoteType.SquareBracket ? null : $"[{identifier.Value}]",
            Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired   => identifier.QuoteType == QuoteType.DoubleQuote ? null : $"\"{identifier.Value}\"",
            Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired => identifier.QuoteType == QuoteType.SquareBracket ? null : $"[{identifier.Value}]",
            Aj5038SettingsNameQuotingPolicy.NotAllowed             => identifier.QuoteType == QuoteType.NotQuoted ? null : identifier.Value,
            _                                                      => throw new ArgumentOutOfRangeException(nameof(quotingPolicy), quotingPolicy, $"{nameof(Aj5038SettingsNameQuotingPolicy)}.{quotingPolicy} is not handled")
        };
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5038",
            IssueType.Formatting,
            "Object name quoting",
            "The `{0}` name segment `{1}` is incorrectly quoted, which violates the configured naming policy. It should be `{2}` instead. Configuration key is `{3}`.",
            ["Object type name", "Name", "Expected name", "Configuration Key name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
