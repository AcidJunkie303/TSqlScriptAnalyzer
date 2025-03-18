using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed record TestCode(string MarkupFreeSql, IReadOnlyList<IIssue> ExpectedIssues);
