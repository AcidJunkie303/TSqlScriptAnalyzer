using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class TabCharacterAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public TabCharacterAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        var sqlCode = _script.ParsedScript.GetSql();
        for (var i = 0; i < sqlCode.Length; i++)
        {
            var c = sqlCode[i];
            if (c != '\t')
            {
                continue;
            }

            var (lineNumber, columnNumber) = sqlCode.GetLineAndColumnNumber(i);

            var codeRegion = CodeRegion.Create(lineNumber, columnNumber, lineNumber, columnNumber + 1);

            var fullObjectName = _script.ParsedScript
                .TryGetSqlFragmentAtPosition(i)
                ?.TryGetFirstClassObjectName(_context, _script);
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(codeRegion.Begin) ?? DatabaseNames.Unknown;
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5008",
            IssueType.Formatting,
            "Tab character",
            "Tab character.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
