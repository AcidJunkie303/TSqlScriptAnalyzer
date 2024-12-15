using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public sealed class WrongUseDatabaseNameAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

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
        foreach (var useStatement in script.ParsedScript.GetDescendantsOfType<SqlUseStatement>())
        {
            if (expectedDatabaseName.EqualsOrdinalIgnoreCase(useStatement.DatabaseName.ToString()))
            {
                continue;
            }

            context.IssueReporter.Report(DiagnosticDefinitions.Default, script, null, useStatement, useStatement.DatabaseName.Value, script.DatabaseName);
        }
    }

    private static bool IsScriptFileExcluded(IAnalysisContext context, string fullScriptFileName)
    {
        var exclusionPatterns = GetExcludedFileNamePatterns(context);
        return exclusionPatterns.Any(a => a.IsMatch(fullScriptFileName));
    }

    private static IReadOnlyCollection<Regex> GetExcludedFileNamePatterns(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5003Settings>();

        return settings.ExcludedFilePathPatterns;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5003",
            IssueType.Warning,
            "Wrong database name in 'USE DATABASE'",
            "Wrong database name in 'USE {0}'. Expected is 'USE {1}'"
        );
    }
}
