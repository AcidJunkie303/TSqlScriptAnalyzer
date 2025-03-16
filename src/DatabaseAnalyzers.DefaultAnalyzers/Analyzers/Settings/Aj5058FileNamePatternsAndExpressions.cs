using System.Text.RegularExpressions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

public sealed record Aj5058FileNamePatternsAndExpressions(
    IReadOnlyList<Regex> Expressions,
    string FlatFileNamePatterns,
    string ShortenedStatementName
);
