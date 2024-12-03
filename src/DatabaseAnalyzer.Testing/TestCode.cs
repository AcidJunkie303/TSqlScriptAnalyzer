using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

public sealed record TestCode(string MarkupFreeSql, IReadOnlyList<IIssue> ExpectedIssues);
