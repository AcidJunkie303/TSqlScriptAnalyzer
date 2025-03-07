using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public sealed class WrongUseDatabaseNameAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var batch = script.ParsedScript.Batches.FirstOrDefault();
        if (batch is null)
        {
            return;
        }

        if (IsScriptFileExcluded(context, script.RelativeScriptFilePath))
        {
            return;
        }

        var expectedDatabaseName = script.DatabaseName;
        foreach (var useStatement in script.ParsedScript.Batches.SelectMany(static a => a.GetChildren<UseStatement>(recursive: true)))
        {
            if (expectedDatabaseName.EqualsOrdinalIgnoreCase(useStatement.DatabaseName.Value))
            {
                continue;
            }

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(useStatement) ?? DatabaseNames.Unknown;
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName: null, useStatement.GetCodeRegion(), useStatement.DatabaseName.Value, script.DatabaseName);
        }
    }

    private static bool IsScriptFileExcluded(IAnalysisContext context, string fullScriptFileName)
    {
        var exclusionPatterns = GetExcludedFileNamePatterns(context);
        return exclusionPatterns.Any(a => a.IsMatch(fullScriptFileName));
    }

    private static IReadOnlyCollection<Regex> GetExcludedFileNamePatterns(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5003Settings>();

        return settings.ExcludedFilePathPatterns;
    }

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
