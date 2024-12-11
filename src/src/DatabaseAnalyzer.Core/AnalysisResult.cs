using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core;

public sealed record AnalysisResult(
    IReadOnlyList<IIssue> Issues,
    IReadOnlyList<IIssue> SuppressedIssues,
    IReadOnlyDictionary<string, IReadOnlyList<IIssue>> IssuesByObjectName,
    IReadOnlySet<string> DisabledDiagnostics
);
