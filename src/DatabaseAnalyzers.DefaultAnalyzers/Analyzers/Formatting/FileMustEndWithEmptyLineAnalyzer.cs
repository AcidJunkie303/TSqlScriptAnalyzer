using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class FileMustEndWithEmptyLineAnalyzer : IScriptAnalyzer
{
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public FileMustEndWithEmptyLineAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _issueReporter = issueReporter;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        if (_script.ParsedScript.ScriptTokenStream.Count < 2)
        {
            return;
        }

        var lastToken = _script.ParsedScript.ScriptTokenStream[^2]; // last tokens is EOF
        if (lastToken.Text?[^1].Equals('\n') == true)
        {
            return;
        }

        var codeRegion = CodeRegion.Create(lastToken.GetCodeLocation(), lastToken.GetCodeRegion().End);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(lastToken.Line, lastToken.Column) ?? DatabaseNames.Unknown;
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName: null, codeRegion);
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
