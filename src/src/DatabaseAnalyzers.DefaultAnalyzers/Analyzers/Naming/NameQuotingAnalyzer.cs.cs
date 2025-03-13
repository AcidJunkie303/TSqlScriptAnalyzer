using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class NameQuotingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5038Settings>();

        AnalyzeObjectCreations(context, script, settings.NameQuotingPolicyDuringObjectCreation, nameof(Aj5038SettingsRaw.NameQuotingPolicyDuringObjectCreation));
        AnalyzeColumnReferences(context, script, settings.NameQuotingPolicyForColumnReferences, nameof(Aj5038SettingsRaw.NameQuotingPolicyForColumnReferences));
        AnalyzeColumnDefinitions(context, script, settings.NameQuotingPolicyForColumnDefinitions, nameof(Aj5038SettingsRaw.NameQuotingPolicyForColumnDefinitions));
        AnalyzeTableReferences(context, script, settings.NameQuotingPolicyForTableReferences, nameof(Aj5038SettingsRaw.NameQuotingPolicyForTableReferences));
        AnalyzeDataTypeReferences(context, script, settings.NameQuotingPolicyForDataTypes, nameof(Aj5038SettingsRaw.NameQuotingPolicyForDataTypes));

        // TODO: Identifiers in object creation (lots of types to check)
        // TODO: Schema name references. Kinda hard...
    }

    private static void AnalyzeDataTypeReferences(IAnalysisContext context, IScriptModel script, Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<SqlDataTypeReference>(recursive: true),
            static a => a.Name.Identifiers,
            "data type",
            policy,
            configurationKeyName);
    }

    private static void AnalyzeTableReferences(IAnalysisContext context, IScriptModel script, Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        var tableReferences = script.ParsedScript
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
            context,
            script,
            tableReferences,
            static a => a.SchemaObject.Identifiers.TakeLast(1),
            "table reference",
            policy,
            configurationKeyName);
    }

    private static void AnalyzeColumnDefinitions(IAnalysisContext context, IScriptModel script, Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ColumnDefinition>(recursive: true),
            static a => [a.ColumnIdentifier],
            "column definition",
            policy,
            configurationKeyName);
    }

    private static void AnalyzeColumnReferences(IAnalysisContext context, IScriptModel script, Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true),
            static a => a.MultiPartIdentifier.Identifiers.TakeLast(1),
            "column",
            policy,
            configurationKeyName);
    }

    private static void AnalyzeObjectCreations(IAnalysisContext context, IScriptModel script, Aj5038SettingsNameQuotingPolicy policy, string configurationKeyName)
    {
        if (policy == Aj5038SettingsNameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true),
            static a => a.SchemaObjectName.Identifiers,
            "table",
            policy,
            configurationKeyName);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ViewStatementBody>(recursive: true),
            static a => a.SchemaObjectName.Identifiers,
            "view",
            policy,
            configurationKeyName);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true),
            static a => a.ProcedureReference.Name.Identifiers,
            "procedure",
            policy,
            configurationKeyName);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true),
            static a => a.Name.Identifiers,
            "function",
            policy,
            configurationKeyName);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<TriggerStatementBody>(recursive: true),
            static a => a.Name.Identifiers,
            "trigger",
            policy,
            configurationKeyName);
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, IEnumerable<T> statements, Func<T, IEnumerable<Identifier>> identifierGetter, string typeName, Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string configurationKeyName)
        where T : TSqlFragment
    {
        foreach (var statement in statements)
        {
            Analyze(context, script, statement, identifierGetter, typeName, aj5038SettingsNameQuotingPolicy, configurationKeyName);
        }
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, T statement, Func<T, IEnumerable<Identifier>> identifierGetter, string typeName, Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string configurationKeyName)
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

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
            var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
            context.IssueReporter.Report(DiagnosticDefinitions.Default,
                databaseName,
                script.RelativeScriptFilePath,
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
