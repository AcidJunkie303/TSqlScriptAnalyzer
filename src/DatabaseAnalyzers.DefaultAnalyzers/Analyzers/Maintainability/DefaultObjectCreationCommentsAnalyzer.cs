using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed partial class DefaultObjectCreationCommentsAnalyzer : IScriptAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly IScriptModel _script;

    public DefaultObjectCreationCommentsAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (Match match in DefaultObjectCreationCommentFinder().Matches(_script.Contents))
        {
            var (startLine, startColumn) = _script.Contents.GetLineAndColumnNumber(match.Index);
            var (endLine, endColumn) = _script.Contents.GetLineAndColumnNumber(match.Index + match.Length);

            var startLocation = CodeLocation.Create(startLine, startColumn);
            var endLocation = CodeLocation.Create(endLine, endColumn);
            var region = CodeRegion.Create(startLocation, endLocation);

            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(startLocation) ?? DatabaseNames.Unknown;
            var fullObjectName = _script.ParsedScript
                .TryGetSqlFragmentAtPosition(match.Index)
                ?.TryGetFirstClassObjectName(_context, _script);
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, region);
        }
    }

    [GeneratedRegex(@"/\*+\s+Object(?!\*+\/).+\*+\/", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex DefaultObjectCreationCommentFinder();

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5047",
            IssueType.Warning,
            "Default Object Creation Comments",
            "Remove the default object creation comments as they are useless",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
