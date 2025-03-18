using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core;

public sealed record AnalysisResult(
    string ScriptsRootDirectoryPath,
    IReadOnlyList<IIssue> Issues,
    IReadOnlyList<SuppressedIssue> SuppressedIssues,
    IReadOnlyDictionary<string, IReadOnlyList<IIssue>> IssuesByObjectName,
    IReadOnlySet<string> DisabledDiagnostics,
    IReadOnlyDictionary<string, IDiagnosticDefinition> DiagnosticDefinitionsById,
    AnalysisResultStatistics Statistics
);
