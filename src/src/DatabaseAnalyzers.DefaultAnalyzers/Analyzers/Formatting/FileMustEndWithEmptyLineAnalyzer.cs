using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class FileMustEndWithEmptyLineAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        if (script.ParsedScript.ScriptTokenStream.Count < 2)
        {
            return;
        }

        var lastToken = script.ParsedScript.ScriptTokenStream[^2]; // last tokens is EOF
        if (lastToken.Text?[^1].Equals('\n') == true)
        {
            return;
        }

        var codeRegion = CodeRegion.Create(lastToken.GetCodeLocation(), lastToken.GetCodeRegion().End);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(lastToken.Line, lastToken.Column) ?? DatabaseNames.Unknown;
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName: null, codeRegion);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5005",
            IssueType.Formatting,
            "File must end with an empty line",
            "File must end with an empty line.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
