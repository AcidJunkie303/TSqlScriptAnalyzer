using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class NameQuotingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5038Settings>();

        AnalyzeObjectCreations(context, script, settings.NameQuotingPolicyDuringObjectCreation);
        AnalyzeColumnReferences(context, script, settings.NameQuotingPolicyForColumnReferences);
    }

    private static void AnalyzeColumnReferences(IAnalysisContext context, IScriptModel script, NameQuotingPolicy policy) => AnalyzeObjectCreations(context, script, policy);

    private static void AnalyzeObjectCreations(IAnalysisContext context, IScriptModel script, NameQuotingPolicy policy)
    {
        if (policy == NameQuotingPolicy.Undefined)
        {
            return;
        }

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<CreateTableStatement>(true),
            a => a.SchemaObjectName.Identifiers,
            "table",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ViewStatementBody>(true),
            a => a.SchemaObjectName.Identifiers,
            "view",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ProcedureStatementBody>(true),
            a => a.ProcedureReference.Name.Identifiers,
            "procedure",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true),
            a => a.Name.Identifiers,
            "function",
            policy);

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<TriggerStatementBody>(recursive: true),
            a => a.Name.Identifiers,
            "trigger",
            policy);
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, IEnumerable<T> statements, Func<T, IList<Identifier>> identifierGetter, string typeName, NameQuotingPolicy nameQuotingPolicy)
        where T : TSqlStatement
    {
        foreach (var statement in statements)
        {
            Analyze(context, script, statement, identifierGetter, typeName, nameQuotingPolicy);
        }
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, T statement, Func<T, IList<Identifier>> identifierGetter, string typeName, NameQuotingPolicy nameQuotingPolicy)
        where T : TSqlStatement
    {
        foreach (var identifier in identifierGetter(statement))
        {
            var shouldBeWrittenAs = GetPolicyCompliantIdentifier(identifier, nameQuotingPolicy);
            if (shouldBeWrittenAs.IsNullOrWhiteSpace())
            {
                continue;
            }

            var databaseName = identifier.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
            var fullObjectName = identifier.TryGetFirstClassObjectName(context, script) ?? "Unknown";
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
            IssueType.Warning,
            "Object name quoting",
            "The {0} '{1}' quoted wrongly which does not comply with the configured policy. It should be: '{2}'."
        );
    }
}
