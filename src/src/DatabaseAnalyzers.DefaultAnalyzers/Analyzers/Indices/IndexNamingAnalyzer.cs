using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class IndexNamingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5052Settings>();

        foreach (var statement in script.ParsedScript.GetChildren<CreateIndexStatement>(recursive: true))
        {
            AnalyzeCreateIndexStatement(context, script, settings, statement);
        }

        foreach (var statement in script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true))
        {
            AnalyzeCreateTableStatement(context, script, settings, statement);
        }
    }

    private static void AnalyzeCreateTableStatement(IAnalysisContext context, IScriptModel script, Aj5052Settings settings, CreateTableStatement statement)
    {
    }

    private static void AnalyzeCreateIndexStatement(IAnalysisContext context, IScriptModel script, Aj5052Settings settings, CreateIndexStatement statement)
    {
        if (!HasSucceedingSiblings(branchExecutionTerminatorStatement, script.ParentFragmentProvider))
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(branchExecutionTerminatorStatement) ?? DatabaseNames.Unknown;
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

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(goToStatement) ?? DatabaseNames.Unknown;
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
            "The code after `{0}` cannot be reached and is considered dead code.",
            ["Statement"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
