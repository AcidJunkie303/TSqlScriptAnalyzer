using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public sealed class WrongUseDatabaseNameAnalyzer : IScriptAnalyzer
{
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5003Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public WrongUseDatabaseNameAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5003Settings settings)
    {
        _issueReporter = issueReporter;
        _settings = settings;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        var batch = _script.ParsedScript.Batches.FirstOrDefault();
        if (batch is null)
        {
            return;
        }

        if (IsScriptFileExcluded(_script.RelativeScriptFilePath))
        {
            return;
        }

        var expectedDatabaseName = _script.DatabaseName;
        foreach (var useStatement in _script.ParsedScript.Batches.SelectMany(static a => a.GetChildren<UseStatement>(recursive: true)))
        {
            if (expectedDatabaseName.EqualsOrdinalIgnoreCase(useStatement.DatabaseName.Value))
            {
                continue;
            }

            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(useStatement) ?? DatabaseNames.Unknown;
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName: null, useStatement.GetCodeRegion(), useStatement.DatabaseName.Value, _script.DatabaseName);
        }
    }

    private bool IsScriptFileExcluded(string fullScriptFileName)
        => _settings.ExcludedFilePathPatterns.Any(a => a.IsMatch(fullScriptFileName));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5003",
            IssueType.Warning,
            "Wrong database name in 'USE' statement",
            "Wrong database name in `USE {0}`. Expected is `USE {1}`.",
            ["Database name used", "Expected database name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
