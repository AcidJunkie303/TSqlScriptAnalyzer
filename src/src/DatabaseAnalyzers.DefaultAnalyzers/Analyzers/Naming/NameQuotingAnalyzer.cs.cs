using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class NameQuotingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5038Settings>();

        AnalyzeObjectCreations(context, script, settings.NameQuotingPolicyDuringObjectCreation);
        AnalyzeColumnReferences(context, script, settings.NameQuotingPolicyForColumnReferences);
        AnalyzeColumnDefinitions(context, script, settings.NameQuotingPolicyForColumnDefinitions);
        AnalyzeTableReferences(context, script, settings.NameQuotingPolicyForTableReferences);
        AnalyzeDataTypeReferences(context, script, settings.NameQuotingPolicyForDataTypes);

        // TODO: Identifiers in object creation (lots of types to check)
        // TODO: Schema name references. Kinda hard...
    }

    private static void AnalyzeDataTypeReferences(IAnalysisContext context, IScriptModel script, NameQuotingPolicy policy)
    {
        if (policy == NameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<SqlDataTypeReference>(recursive: true),
            static a => a.Name.Identifiers,
            "data type",
            policy);
    }

    private static void AnalyzeTableReferences(IAnalysisContext context, IScriptModel script, NameQuotingPolicy policy)
    {
        if (policy == NameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<NamedTableReference>(recursive: true),
            static a => a.SchemaObject.Identifiers.TakeLast(1),
            "table reference",
            policy);
    }

    private static void AnalyzeColumnDefinitions(IAnalysisContext context, IScriptModel script, NameQuotingPolicy policy)
    {
        if (policy == NameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ColumnDefinition>(recursive: true),
            static a => [a.ColumnIdentifier],
            "column definition",
            policy);
    }

    private static void AnalyzeColumnReferences(IAnalysisContext context, IScriptModel script, NameQuotingPolicy policy)
    {
        if (policy == NameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true),
            static a => a.MultiPartIdentifier.Identifiers.TakeLast(1),
            "column",
            policy);
    }

    private static void AnalyzeObjectCreations(IAnalysisContext context, IScriptModel script, NameQuotingPolicy policy)
    {
        if (policy == NameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true),
            static a => a.SchemaObjectName.Identifiers,
            "table",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ViewStatementBody>(recursive: true),
            static a => a.SchemaObjectName.Identifiers,
            "view",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true),
            static a => a.ProcedureReference.Name.Identifiers,
            "procedure",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true),
            static a => a.Name.Identifiers,
            "function",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<TriggerStatementBody>(recursive: true),
            static a => a.Name.Identifiers,
            "trigger",
            policy);
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, IEnumerable<T> statements, Func<T, IEnumerable<Identifier>> identifierGetter, string typeName, NameQuotingPolicy nameQuotingPolicy)
        where T : TSqlFragment
    {
        foreach (var statement in statements)
        {
            Analyze(context, script, statement, identifierGetter, typeName, nameQuotingPolicy);
        }
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, T statement, Func<T, IEnumerable<Identifier>> identifierGetter, string typeName, NameQuotingPolicy nameQuotingPolicy)
        where T : TSqlFragment
    {
        foreach (var identifier in identifierGetter(statement))
        {
            var shouldBeWrittenAs = GetPolicyCompliantIdentifier(identifier, nameQuotingPolicy);
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
                typeName, identifier.GetSql(), shouldBeWrittenAs);
        }
    }

    private static string? GetPolicyCompliantIdentifier(Identifier identifier, NameQuotingPolicy quotingPolicy)
    {
        if (identifier.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        return quotingPolicy switch
        {
            NameQuotingPolicy.Undefined => null,
            NameQuotingPolicy.Required => identifier.QuoteType is QuoteType.DoubleQuote or QuoteType.SquareBracket ? null : $"[{identifier.Value}]",
            NameQuotingPolicy.DoubleQuotesRequired => identifier.QuoteType == QuoteType.DoubleQuote ? null : $"\"{identifier.Value}\"",
            NameQuotingPolicy.SquareBracketsRequired => identifier.QuoteType == QuoteType.SquareBracket ? null : $"[{identifier.Value}]",
            NameQuotingPolicy.NotAllowed => identifier.QuoteType == QuoteType.NotQuoted ? null : identifier.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(quotingPolicy), quotingPolicy, $"{nameof(NameQuotingPolicy)}.{quotingPolicy} is not handled")
        };
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5038",
            IssueType.Formatting,
            "Object name quoting",
            "The {0} name part `{1}` is quoted wrongly which does not comply with the configured policy. It should be: `{2}`.",
            ["Object type name", "Name", "Expected name"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
