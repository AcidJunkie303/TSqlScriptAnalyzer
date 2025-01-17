using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed partial class DefaultObjectCreationCommentsAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (Match match in DefaultObjectCreationCommentFinder().Matches(script.Contents))
        {
            var (startLine, startColumn) = script.Contents.GetLineAndColumnNumber(match.Index);
            var (endLine, endColumn) = script.Contents.GetLineAndColumnNumber(match.Index + match.Length);

            var startLocation = CodeLocation.Create(startLine, startColumn);
            var endLocation = CodeLocation.Create(endLine, endColumn);
            var region = CodeRegion.Create(startLocation, endLocation);

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(startLocation) ?? DatabaseNames.Unknown;
            var fullObjectName = script.ParsedScript
                .TryGetSqlFragmentAtPosition(match.Index)
                ?.TryGetFirstClassObjectName(context, script);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, region);
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
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
