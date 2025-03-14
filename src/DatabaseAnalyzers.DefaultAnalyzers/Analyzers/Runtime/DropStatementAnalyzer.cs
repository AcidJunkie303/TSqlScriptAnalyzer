using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class DropStatementAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5058Settings>();

        foreach (var statement in script.ParsedScript.GetChildren<TSqlStatement>(recursive: true))
        {
            AnalyzeStatement(context, script, settings, statement);
        }
    }

    private static void AnalyzeStatement(IAnalysisContext context, IScriptModel script, Aj5058Settings settings, TSqlStatement statement)
    {
        var expressionsAndPatterns = settings.AllowedInFilesByDropStatementType.GetValueOrDefault(statement.GetType());
        if (expressionsAndPatterns is null)
        {
            return;
        }

        var (expressions, allowedFileNamePatterns, shortStatementName) = expressionsAndPatterns;
        if (expressions.Any(a => a.IsMatch(script.RelativeScriptFilePath)))
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion(), shortStatementName, allowedFileNamePatterns);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5058",
            IssueType.Warning,
            "Drop statement in non-allowed file",
            "The drop statement `{0}` is not allowed in this file. It is only allowed in files with the following name pattern(s): `{1}`",
            ["Drop Statement Type", "Allowed file name patterns"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
