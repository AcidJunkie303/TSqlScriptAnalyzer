using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingNoCountInProcedureOrTriggerAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingNoCountInProcedureOrTriggerAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var creationStatement in _script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true))
        {
            var bodyStatements = GetStatements(creationStatement.StatementList);
            Analyze(creationStatement, bodyStatements);
        }

        foreach (var creationStatement in _script.ParsedScript.GetChildren<TriggerStatementBody>(recursive: true))
        {
            var bodyStatements = GetStatements(creationStatement.StatementList);
            Analyze(creationStatement, bodyStatements);
        }
    }

    private void Analyze(TSqlStatement creationStatement, IList<TSqlStatement> bodyStatements)
    {
        if (bodyStatements.IsNullOrEmpty())
        {
            return;
        }

        var setOptionStatements = bodyStatements
            .TakeWhile(static a => a is PredicateSetStatement)
            .Cast<PredicateSetStatement>()
            .ToList();

        if (setOptionStatements.Count > 0 || setOptionStatements.Any(static a => a.IsOn && a.Options.HasFlag(SetOptions.NoCount)))
        {
            return;
        }

        var firstStatement = bodyStatements[0];
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(creationStatement) ?? DatabaseNames.Unknown;
        var fullObjectName = creationStatement.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, firstStatement.GetCodeRegion());
    }

    private static IList<TSqlStatement> GetStatements(StatementList? statementList)
    {
        while (true)
        {
            var statements = statementList?.Statements;
            if (statements.IsNullOrEmpty())
            {
                return [];
            }

            if (statements.Count == 1 && statements[0] is BeginEndBlockStatement beginEndBlockStatement)
            {
                statementList = beginEndBlockStatement.StatementList;
                continue;
            }

            return statements;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5029",
            IssueType.Warning,
            "The first statement in a procedure should be 'SET NOCOUNT ON'",
            "The first statement in a procedure should be `SET NOCOUNT ON`.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
