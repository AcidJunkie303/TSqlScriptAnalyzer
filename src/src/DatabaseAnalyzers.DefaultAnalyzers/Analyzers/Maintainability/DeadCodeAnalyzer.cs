using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class DeadCodeAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        AnalyzeStatements<BreakStatement>(context, script, "BREAK");
        AnalyzeStatements<ContinueStatement>(context, script, "CONTINUE");
        AnalyzeStatements<ReturnStatement>(context, script, "RETURN");
        AnalyzeStatements<ThrowStatement>(context, script, "THROW");

        AnalyzeGoToStatements(context, script);
    }

    private static void AnalyzeStatements<T>(IAnalysisContext context, IScriptModel script, string statementName)
        where T : TSqlStatement
    {
        foreach (var statement in script.ParsedScript.GetChildren<T>(recursive: true))
        {
            AnalyzeStatement(context, script, statement, statementName);
        }
    }

    private static void AnalyzeStatement(IAnalysisContext context, IScriptModel script, TSqlFragment branchExecutionTerminatorStatement, string statementName)
    {
        if (!HasSucceedingSiblings(branchExecutionTerminatorStatement, script.ParentFragmentProvider))
        {
            return;
        }

        var databaseName = branchExecutionTerminatorStatement.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = branchExecutionTerminatorStatement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, branchExecutionTerminatorStatement.GetCodeRegion(), statementName);

        static bool HasSucceedingSiblings(TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
            => fragment.GetSucceedingSiblings(parentFragmentProvider).Any();
    }

    private static void AnalyzeGoToStatements(IAnalysisContext context, IScriptModel script)
    {
        foreach (var statement in script.ParsedScript.GetChildren<GoToStatement>(recursive: true))
        {
            AnalyzeGoToStatement(context, script, statement);
        }
    }

    private static void AnalyzeGoToStatement(IAnalysisContext context, IScriptModel script, GoToStatement goToStatement)
    {
        foreach (var batch in script.ParsedScript.Batches)
        {
            AnalyzeGoToStatement(context, script, batch, goToStatement);
        }
    }

    private static void AnalyzeGoToStatement(IAnalysisContext context, IScriptModel script, TSqlBatch batch, GoToStatement goToStatement)
    {
        if (!IsCodeAfterGotoDead(script, batch, goToStatement))
        {
            return;
        }

        var databaseName = goToStatement.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = goToStatement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, goToStatement.GetCodeRegion(), "GOTO");
    }

    private static bool IsCodeAfterGotoDead(IScriptModel script, TSqlBatch batch, GoToStatement goToStatement)
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

        var countOfSucceedingFragments = goToStatement.GetSucceedingSiblings(script.ParentFragmentProvider).Count();
        if (countOfSucceedingFragments == 0)
        {
            return false;
        }

        var succeedingSiblingLabels = goToStatement
            .GetSucceedingSiblings(script.ParentFragmentProvider)
            .OfType<LabelStatement>()
            .ToList();

        var isTargetLabelSucceedingSibling = succeedingSiblingLabels.Exists(a => a == targetLabel);
        if (isTargetLabelSucceedingSibling)
        {
            var countOfStatementsBetweenGotoAndNextLabel = goToStatement
                .GetSucceedingSiblings(script.ParentFragmentProvider)
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
            "The code after '{0}' cannot be reached and is considered dead code."
        );
    }
}
