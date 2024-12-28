using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingNoCountInProcedureOrTriggerAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var creationStatement in script.ParsedScript.GetChildren<ProcedureStatementBody>(true))
        {
            var bodyStatements = GetStatements(creationStatement.StatementList);
            Analyze(context, script, creationStatement, bodyStatements);
        }

        foreach (var creationStatement in script.ParsedScript.GetChildren<TriggerStatementBody>(true))
        {
            var bodyStatements = GetStatements(creationStatement.StatementList);
            Analyze(context, script, creationStatement, bodyStatements);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, TSqlStatement creationStatement, IList<TSqlStatement> bodyStatements)
    {
        if (bodyStatements.IsNullOrEmpty())
        {
            return;
        }

        var setOptionStatements = bodyStatements
            .TakeWhile(a => a is PredicateSetStatement)
            .Cast<PredicateSetStatement>()
            .ToList();

        if (setOptionStatements.Count > 0 || setOptionStatements.Any(a => a.IsOn && a.Options.HasFlag(SetOptions.NoCount)))
        {
            return;
        }

        var firstStatement = bodyStatements[0];
        var databaseName = creationStatement.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = creationStatement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, firstStatement.GetCodeRegion());
    }

    private static IList<TSqlStatement> GetStatements(StatementList? statementList)
    {
        var statements = statementList?.Statements;
        if (statements.IsNullOrEmpty())
        {
            return [];
        }

        if (statements.Count == 1 && statements[0] is BeginEndBlockStatement beginEndBlockStatement)
        {
            return GetStatements(beginEndBlockStatement.StatementList);
        }

        return statements;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5029",
            IssueType.Warning,
            "The first statement in a procedure should be 'SET NOCOUNT ON'",
            "The first statement in a procedure should be 'SET NOCOUNT ON'."
        );
    }
}
