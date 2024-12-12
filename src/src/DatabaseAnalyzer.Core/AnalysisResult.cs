using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core;

public sealed record AnalysisResult(
    IReadOnlyList<IIssue> Issues,
    IReadOnlyList<SuppressedIssue> SuppressedIssues,
    IReadOnlyDictionary<string, IReadOnlyList<IIssue>> IssuesByObjectName,
    IReadOnlySet<string> DisabledDiagnostics
);
