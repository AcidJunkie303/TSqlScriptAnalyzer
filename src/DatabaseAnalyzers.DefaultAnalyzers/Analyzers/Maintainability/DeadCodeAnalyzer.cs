using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class DeadCodeAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public DeadCodeAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        AnalyzeStatements<BreakStatement>("BREAK");
        AnalyzeStatements<ContinueStatement>("CONTINUE");
        AnalyzeStatements<ReturnStatement>("RETURN");
        AnalyzeStatements<ThrowStatement>("THROW");

        AnalyzeGoToStatements();
    }

    private void AnalyzeStatements<T>(string statementName)
        where T : TSqlStatement
    {
        foreach (var statement in _script.ParsedScript.GetChildren<T>(recursive: true))
        {
            AnalyzeStatement(statement, statementName);
        }
    }

    private void AnalyzeStatement(TSqlFragment branchExecutionTerminatorStatement, string statementName)
    {
        if (!HasSucceedingSiblings(branchExecutionTerminatorStatement, _script.ParentFragmentProvider))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(branchExecutionTerminatorStatement) ?? DatabaseNames.Unknown;
        var fullObjectName = branchExecutionTerminatorStatement.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, branchExecutionTerminatorStatement.GetCodeRegion(), statementName);

        static bool HasSucceedingSiblings(TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
            => fragment.GetSucceedingSiblings(parentFragmentProvider).Any();
    }

    private void AnalyzeGoToStatements()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<GoToStatement>(recursive: true))
        {
            AnalyzeGoToStatement(statement);
        }
    }

    private void AnalyzeGoToStatement(GoToStatement goToStatement)
    {
        foreach (var batch in _script.ParsedScript.Batches)
        {
            AnalyzeGoToStatement(batch, goToStatement);
        }
    }

    private void AnalyzeGoToStatement(TSqlBatch batch, GoToStatement goToStatement)
    {
        if (!IsCodeAfterGotoDead(batch, goToStatement))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(goToStatement) ?? DatabaseNames.Unknown;
        var fullObjectName = goToStatement.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, goToStatement.GetCodeRegion(), "GOTO");
    }

    private bool IsCodeAfterGotoDead(TSqlBatch batch, GoToStatement goToStatement)
    {
        // The code after GOTO is only dead if the siblings after the GOTO statement aren't any other labels than the target label
        var labelName = goToStatement.LabelName?.Value.TrimEnd(':') + ':'; // we make sure that it ends with a colon
        if (labelName.IsNullOrWhiteSpace())
        {
            return false;
        }

        var targetLabel = batch
            .GetChildren<LabelStatement>(recursive: true)
            .SingleOrDefault(a => a.Value.EqualsOrdinalIgnoreCase(labelName));

        if (targetLabel is null)
        {
            return false;
        }

        var countOfSucceedingFragments = goToStatement.GetSucceedingSiblings(_script.ParentFragmentProvider).Count();
        if (countOfSucceedingFragments == 0)
        {
            return false;
        }

        var succeedingSiblingLabels = goToStatement
            .GetSucceedingSiblings(_script.ParentFragmentProvider)
            .OfType<LabelStatement>()
            .ToList();

        var isTargetLabelSucceedingSibling = succeedingSiblingLabels.Exists(a => a == targetLabel);
        if (isTargetLabelSucceedingSibling)
        {
            var countOfStatementsBetweenGotoAndNextLabel = goToStatement
                .GetSucceedingSiblings(_script.ParentFragmentProvider)
                .TakeWhile(a => a is not LabelStatement)
                .Count();

            if (countOfStatementsBetweenGotoAndNextLabel == 0)
            {
                return false;
            }
        }

        return true;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5035",
            IssueType.Warning,
            "Dead Code",
            "The code after `{0}` cannot be reached and is considered dead code.",
            ["Statement"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
