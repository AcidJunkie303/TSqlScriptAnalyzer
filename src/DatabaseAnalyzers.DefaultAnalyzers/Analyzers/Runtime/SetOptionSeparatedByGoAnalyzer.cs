using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class SetOptionSeparatedByGoAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public SetOptionSeparatedByGoAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var group in GetBatchGroupsWhichCanBeCombined(_script.ParsedScript))
        {
            var firstBatchCodeRegion = group[0].GetCodeRegion();
            var lastBatchCodeRegion = group[^1].GetCodeRegion();

            var codeRegion = CodeRegion.CreateSpan(firstBatchCodeRegion, lastBatchCodeRegion);
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(group[0]) ?? DatabaseNames.Unknown;
            var fullObjectName = group[0].TryGetFirstClassObjectName(_context, _script);
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion);
        }
    }

    private static IEnumerable<List<TSqlBatch>> GetBatchGroupsWhichCanBeCombined(TSqlScript script)
    {
        var groups = new List<List<TSqlBatch>>(10);
        var currentGroup = new List<TSqlBatch>();
        groups.Add(currentGroup);

        foreach (var batch in script.GetChildren<TSqlBatch>(recursive: true))
        {
            var isBatchUsingSetOptionsOnly = IsBatchUsingSetOptionsOnly(batch);
            if (isBatchUsingSetOptionsOnly)
            {
                currentGroup.Add(batch);
            }
            else
            {
                currentGroup = [];
                groups.Add(currentGroup);
            }
        }

        return groups.Where(static a => a.Count > 1);
    }

    private static bool IsBatchUsingSetOptionsOnly(TSqlBatch batch)
        => batch.GetChildren().All(static a => a is PredicateSetStatement);

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5034",
            IssueType.Warning,
            "Set options don't need to be separated by GO",
            "Multiple set option calls are not required to be separated by `GO`. Use one GO statement at the end  of multiple set option calls.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
